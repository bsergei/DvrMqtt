using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using Dvr.Commands;
using Dvr.Commands.AdHoc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dvr;

public partial class DvrIpWebService : IDvrIpWebService, IDisposable
{
    private const int DefaultPort = 34567;
    private const string DefaultUserName = "admin";
    private const string DefaultPassWord = "tlJwpbo6"; // This value is empty password hashed by XM magic.

    private static readonly MethodInfo SendGenericMethodInfo = typeof(DvrIpWebService)
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(m => m.Name == nameof(Send) && m.GetGenericArguments().Length == 2);

    /// <summary>
    /// Cache for generic send methods. It is useful to have ability to call just
    /// <c>Send(cmd);</c> instead of <c>Send;lt;ReqCmdType, ReplyCmdType&gt;(cmd);</c>.
    /// It can't be static due to captured "this".
    /// </summary>
    private readonly ConcurrentDictionary<Type, Delegate> sendGenericFunctionsCache_ = new();
    
    private static readonly TimeSpan SendReceiveTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ReceiverBufferItemTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Cancellation token for all background tasks and operations.
    /// </summary>
    private CancellationTokenSource? cts_;
    
    /// <summary>
    /// Store token to avoid ObjectDisposedException when get token directly from cts_ each time.
    /// </summary>
    private readonly CancellationToken cancellationToken_; 

    /// <summary>
    /// Useful to await state when DVR ready to send/receive commands.
    /// </summary>
    private readonly TaskCompletionSource whenConnectedTcs_ = new();
    
    /// <summary>
    /// Synchronization for send/receive.
    /// DVR is not working well with parallel requests.
    /// </summary>
    private SemaphoreSlim? sendReceiveSync_ = new(1, 1); 
    
    private SemaphoreSlim? apiSync_ = new(1, 1); 

    private readonly DvrOptions options_;
    
    private readonly IDvrIpPacket dvrIpPacket_;
    private readonly ILogger<DvrIpWebService> logger_;

    /// <summary>
    /// DataFlow block for socket received data.
    /// </summary>
    private readonly BufferBlock<(byte[] DataBytes, DateTime ReceivedTimeStamp)> receiverBufferBlock_ = new();
    
    private Socket? socket_;

    /// <summary>
    /// Command sequence number.
    /// </summary>
    private uint cmdSeq_;

    /// <summary>
    /// Current logged in session id.
    /// </summary>
    private uint sessionId_;
    
    private int keepAliveIntervalInSeconds_ = 20;

    public DvrIpWebService(
        IOptions<DvrOptions> options,
        IDvrIpPacket dvrIpPacket,
        ILogger<DvrIpWebService> logger)
    {
        options_ = options.Value;
        dvrIpPacket_ = dvrIpPacket;
        logger_ = logger;
        cts_ = new CancellationTokenSource();
        cancellationToken_ = cts_.Token;
    }

    public void Dispose()
    {
        using var cts = Interlocked.Exchange(ref cts_, null);
        using var socket = Interlocked.Exchange(ref socket_, null);
     
        cts?.Cancel();
        socket?.Close();

        // Just to log any timeout commands.
        CleanupResponseBuffer(true);

        Interlocked.Exchange(ref sendReceiveSync_, null)?.Dispose();
        Interlocked.Exchange(ref apiSync_, null)?.Dispose();
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        var registration = cancellationToken.Register(() => cts_!.Cancel());
        try
        {
            Task receiverTask;
            Task keepAliveTask;

            try
            {
                await SocketConnect();
                receiverTask = RunSocketReceive();

                await Login();
                keepAliveTask = RunKeepAlive();

                whenConnectedTcs_.SetResult();
            }
            catch (Exception exception)
            {
                whenConnectedTcs_.SetException(exception);
                throw;
            }

            var bgTasks = new List<Task> { receiverTask, keepAliveTask };
            while (bgTasks.Count > 0)
            {
                var task = await Task.WhenAny(receiverTask, keepAliveTask);
                bgTasks.Remove(task);

                if (task.Exception != null)
                {
                    throw new Exception("Task failed", task.Exception);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            logger_.LogError(exception, "DVR Run task exited with error");
            throw;
        }
        finally
        {
            await registration.DisposeAsync();
        }
    }

    public async Task WhenConnected()
    {
        await whenConnectedTcs_.Task;
    }

    public IObservable<AlarmInfo> ObserveAlarms()
    {
        return Observable.Create<AlarmInfo>(async observer =>
        {
            await Send(new GuardRequest());

            var unsubscribeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken_);

            var id = Guid.NewGuid();

            logger_.LogInformation($"ObserveAlarms started {id}");
            HandleAlarmInfos(observer, unsubscribeCts.Token)
                .ContinueWith(_ => logger_.LogInformation($"ObserveAlarms finished {id}"))
                .Forget();

            return async () =>
            {
                try
                {
                    unsubscribeCts.Cancel();

                    try
                    {
                        await Send(new UnGuardRequest());
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        logger_.LogDebug(exception, $"ObserveAlarms un-subscriber failed");
                    }
                }
                finally
                {
                    unsubscribeCts.Dispose();
                }
            };
        });
    }

    public async Task SystemRequest(OpMachine opMachine)
    {
        EnsureLoggedIn();

        var cmd = new SystemRequest()
        {
            OpMachine = opMachine
        };

        await Send(cmd);
    }

    public async Task<JsonElement> GetConfig(string name)
    {
        EnsureLoggedIn();

        var cmd = new GetConfigRequest
        {
            Name = name
        };

        var reply = await Send(cmd);
        return reply.Data;
    }

    public async Task SetConfig(string name, JsonElement data)
    {
        EnsureLoggedIn();

        var cmd = new SetConfigRequest
        {
            Data = data.Clone(),
            Name = name
        };

        await Send(cmd);
    }

    private async Task SocketConnect()
    {
        socket_?.Dispose();
        sessionId_ = 0;
        cmdSeq_ = 0;

        var ipEndPoint = GetIpEndPoint();

        socket_ = new Socket(
            ipEndPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);

        await socket_.ConnectAsync(ipEndPoint, cancellationToken_);
    }

    private enum ReaderState
    {
        ReadStartSequence,
        ReadHeader,
        ReadData
    }

    private async Task RunSocketReceive()
    {
        try
        {
            if (socket_ == null)
            {
                throw new InvalidOperationException($"Not connected");
            }

            var buffer = new byte[1024];

            const int HeaderSize = 20;
            const int DataLengthStart = 16;
            const int DataLengthEnd = 20;

            var response = new List<byte>(1024);
            var packetStartSequence = new byte[] { 0xFF, 0x01 };
            var seqIndex = 0;
            uint dataLength = 0;
            var state = ReaderState.ReadStartSequence;

            while (!cancellationToken_.IsCancellationRequested)
            {
                int length;
                try
                {
                    length = await socket_.ReceiveAsync(buffer, cancellationToken_);
                }
                catch (OperationCanceledException)
                {
                    continue;
                }

                if (cancellationToken_.IsCancellationRequested)
                {
                    continue;
                }

                for (var bufferIndex = 0; bufferIndex < length; bufferIndex++)
                {
                    response.Add(buffer[bufferIndex]);

                    switch (state)
                    {
                        case ReaderState.ReadStartSequence:
                            if (TryReadSequenceWithAdvance(
                                    buffer, bufferIndex, 
                                    packetStartSequence, ref seqIndex))
                            {
                                state = ReaderState.ReadHeader;
                            }
                            break;
                    
                        case ReaderState.ReadHeader:
                            if (response.Count == HeaderSize)
                            {
                                dataLength = BitConverter.ToUInt32(
                                    CollectionsMarshal.AsSpan(response)[DataLengthStart..DataLengthEnd]);

                                state = ReaderState.ReadData;
                            }
                            break;
                    
                        case ReaderState.ReadData:
                            if (dataLength > 0)
                            {
                                dataLength--;
                            }

                            if (dataLength == 0)
                            {
                                state = ReaderState.ReadStartSequence;
                                
                                seqIndex = 0;
                                dataLength = 0;

                                PostDataToReceiverBufferBlock(response.ToArray());
                                response.Clear();
                            }
                            break;
                    
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger_.LogError(e, $"Receiver task died due to unhandled error");
            throw;
        }
    }

    private bool TryReadSequenceWithAdvance(byte[] buffer, int i, byte[] seq, ref int seqIndex)
    {
        if (buffer[i] == seq[seqIndex])
        {
            seqIndex++;
            if (seqIndex >= seq.Length)
            {
                return true;
            }
        }
        else
        {
            seqIndex = 0;
        }

        return false;
    }

    private async Task Login()
    {
        if (sessionId_ != 0)
        {
            throw new InvalidOperationException($"Already logged in");
        }

        sessionId_ = 0;
        cmdSeq_ = 0;

        var reply = await Send(new LoginRequest
        {
            UserName = GetUserName(),
            PassWord = GetPassWord(),
        });

        if (!UInt32.TryParse(reply.SessionID?[2..] ?? String.Empty, NumberStyles.HexNumber, null, out var sessionId))
        {
            throw new InvalidOperationException($"Invalid SessionID received by login: {reply.SessionID}");
        }

        sessionId_ = sessionId;
        keepAliveIntervalInSeconds_ = reply.AliveInterval;
        if (keepAliveIntervalInSeconds_ == 0)
        {
            keepAliveIntervalInSeconds_ = 20;
        }
    }

    private async Task RunKeepAlive()
    {
        try
        {
            EnsureLoggedIn();

            while (!cancellationToken_.IsCancellationRequested)
            {
                try
                {
                    await Send(new KeepAliveRequest());
                    await Task.Delay(TimeSpan.FromSeconds(keepAliveIntervalInSeconds_), cancellationToken_);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
        catch (Exception e)
        {
            logger_.LogError(e, $"Keep-alive task died due to error");
            throw;
        }
    }

    private async Task HandleAlarmInfos(
        IObserver<AlarmInfo> alarmObserver,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await foreach (var (tuple, exception) in ReceiveReplies<AlarmInfoDvrRequest>(cancellationToken))
            {
                if (exception == null && tuple != null)
                {
                    var alarmInfo = tuple.Value.Item?.AlarmInfo;
                    if (alarmInfo != null)
                    {
                        alarmObserver.OnNext(alarmInfo);
                    }
                } 
                else if (exception != null)
                {
                    // alarmObserver.OnError(exception);
                    logger_.LogError(exception, $"Ignoring error in HandleAlarmInfos");
                }
            }
        }

        alarmObserver.OnCompleted();
    }
    private IPEndPoint GetIpEndPoint()
    {
        var host = options_.HostIp ?? throw new ArgumentException($"Host cannot be null");
        var port = options_.Port ?? DefaultPort;
        

        var ipBytes = host.Split('.').Select(byte.Parse).ToArray();
        var ipAddress = new IPAddress(ipBytes);
        var ipEndPoint = new IPEndPoint(ipAddress, port);
        return ipEndPoint;
    }

    private string GetUserName() => 
        options_.User ?? DefaultUserName;

    private string GetPassWord() =>
        String.IsNullOrEmpty(options_.Password)
            ? DefaultPassWord
            : CalcXmHash(options_.Password);

    private T EnsureSuccessfulReply<T>(
        T reply,
        [CallerMemberName] string? method = null) where T : IDvrReply
    {
        var retFound = RetValues.ErrorLookup.TryGetValue(reply.Ret, out var errorRet);
        if (retFound && errorRet.Success)
        {
            return reply;
        }
        else
        {
            if (retFound)
            {
                throw new InvalidOperationException($"{method} command failed: {errorRet.Message}");
            }
            else
            {
                throw new InvalidOperationException($"{method} command failed: Ret={reply.Ret}");
            }
        }
    }

    private (byte[] Bytes, uint Seq) CreatePacket<T>(T cmd)
    {
        var seq = Interlocked.Increment(ref cmdSeq_);
        return (
            dvrIpPacket_.CreatePacket(cmd, seq: seq, session: sessionId_),
            seq
        );
    }

    private string GetSessionIdHex() => $"0x{sessionId_:X}";

    private void EnsureLoggedIn()
    {
        if (sessionId_ == 0)
        {
            throw new InvalidOperationException($"Not logged in");
        }
    }

    private Task<TReply> Send<TReply>(IDvrRequest<TReply> request)
        where TReply : IDvrReply
    {
        return GetSendFunc<TReply>(request.GetType())(request);
    }

    private Func<IDvrRequest<TReply>, Task<TReply>> GetSendFunc<TReply>(Type requestType)
        where TReply : IDvrReply
    {
        var sendFunc = sendGenericFunctionsCache_.GetOrAdd(requestType, _ =>
        {
            var methodClosed = SendGenericMethodInfo.MakeGenericMethod(requestType, typeof(TReply));
            var funcTypeClosed = typeof(Func<,>).MakeGenericType(
                requestType, 
                typeof(Task<>).MakeGenericType(typeof(TReply)));

            var f = Delegate.CreateDelegate(funcTypeClosed, this, methodClosed);
            return f;
        });

        return req => (Task<TReply>)sendFunc.DynamicInvoke(req)!;
    }

    private async Task<TReply> Send<TRequest, TReply>(TRequest request)
        where TRequest : IDvrRequest<TReply>
        where TReply : IDvrReply
    {
        cancellationToken_.ThrowIfCancellationRequested();
        if (sendReceiveSync_ == null)
        {
            throw new ObjectDisposedException(nameof(DvrIpWebService));
        }

        await sendReceiveSync_.WaitAsync(cancellationToken_);
        try
        {
            var seq = await SendRequest(request);
            var reply = await ReceiveReply<TReply>(seq);
            return EnsureSuccessfulReply(reply);
        }
        finally
        {
            sendReceiveSync_?.Release();
        }
    }

    private async Task<uint> SendRequest<T>(T cmd)
    {
        if (cmd is IDvrRequest dvrCommand)
        {
            dvrCommand.SessionID = GetSessionIdHex();
        }

        var (bytes, seq) = CreatePacket(cmd);

        await SendRequest(bytes);

        return seq;
    }

    private async Task SendRequest(byte[] bytes)
    {
        if (socket_ == null)
        {
            throw new InvalidOperationException($"Not connected");
        }

        cancellationToken_.ThrowIfCancellationRequested();

        using var timeoutCts = new CancellationTokenSource(SendReceiveTimeout);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken_, timeoutCts.Token);

        try
        {
            await socket_.SendAsync(bytes, cts.Token);
        }
        catch (OperationCanceledException)
        {
            if (timeoutCts.IsCancellationRequested)
            {
                dvrIpPacket_.TryGetSeq(bytes, out var seq);
                dvrIpPacket_.TryGetCommandId(bytes, out var cmdId);
                throw new InvalidOperationException($"Send command timeout (sessionId={sessionId_}, cmdId={cmdId}, seq={seq})");
            }

            throw;
        }
    }

    private async Task<T> ReceiveReply<T>(uint seq)
    {
        return (await ReceiveReplyEx<T>(seq)).Item;
    }

    private async Task<(T Item, uint SessionID)> ReceiveReplyEx<T>(uint seq)
    {
        cancellationToken_.ThrowIfCancellationRequested();

        using var timeoutCts = new CancellationTokenSource(SendReceiveTimeout);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken_, 
            timeoutCts.Token);

        var cancellationToken = cts.Token;

        try
        {
            return await ReceiveOneReply<T>(seq, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (timeoutCts.IsCancellationRequested)
            {
                throw new InvalidOperationException($"Receive command timeout for {typeof(T).Name} (sessionId={sessionId_}, seq={seq})");
            }

            throw;
        }
    }

    private async Task<(T Item, uint SessionID)> ReceiveOneReply<T>(uint? seq, CancellationToken cancellationToken)
    {
        var responseBytes = await ReceiveOneReplyBytes<T>(seq, cancellationToken);
        return dvrIpPacket_.ParsePacket<T>(responseBytes, seq);
    }

    private async Task<T> ReceiveOneReplyData<T>(uint? seq, CancellationToken cancellationToken) where T : IDvrDataReply, new()
    {
        var responseBytes = await ReceiveOneReplyBytes<T>(seq, cancellationToken);
        return new T { Data = responseBytes };
    }

    private async Task<byte[]> ReceiveOneReplyBytes<T>(uint? seq, CancellationToken cancellationToken)
    {
        var propagator = new WriteOnceBlock<(byte[] DataBytes, DateTime ReceivedTimeStamp)>(
            v => (v.DataBytes, v.ReceivedTimeStamp));

        using var link = receiverBufferBlock_.LinkTo(
            propagator,
            v => CanReceiveOne<T>(v.DataBytes, seq));

        var (responseBytes, _) = await propagator.ReceiveAsync(cancellationToken);
        return responseBytes;
    }

    private async IAsyncEnumerable<((T Item, uint SessionID)? Item, Exception? error)> ReceiveReplies<T>(
        [EnumeratorCancellation]CancellationToken cancellationToken)
    {
        var propagator = new BufferBlock<(byte[] DataBytes, DateTime ReceivedTimeStamp)>();

        using var __ = receiverBufferBlock_.LinkTo(
            propagator, 
            v => CanReceiveOne<T>(v.DataBytes, null));

        while (!cancellationToken.IsCancellationRequested)
        {
            (T Data, uint SessionId)? tuple = null;
            Exception? exception = null;
            try
            {
                var (responseBytes, _) = await propagator.ReceiveAsync(cancellationToken);
                tuple = dvrIpPacket_.ParsePacket<T>(responseBytes);
            }
            catch (OperationCanceledException)
            {
                continue;
            }
            catch (Exception e)
            {
                exception = e;
            }

            yield return (tuple, exception);
        }
    }

    private bool CanReceiveOne<T>(byte[] dataBytes, uint? seq)
    {
        return
            (
                seq == null
                || (dvrIpPacket_.TryGetSeq(dataBytes, out var receivedSeq)
                    && receivedSeq == seq)
            )
            && (
                sessionId_ == 0
                || (
                    dvrIpPacket_.TryGetSessionId(dataBytes, out var receivedSessionId)
                    && receivedSessionId == sessionId_
                )
            )
            && (
                DvrCommandIdAttribute.GetCommandId<T>() == null
                || (
                    dvrIpPacket_.TryGetCommandId(dataBytes, out var receivedCommandId)
                    && receivedCommandId == DvrCommandIdAttribute.GetCommandId<T>()
                )
            );
    }

    private void PostDataToReceiverBufferBlock(byte[] bytes)
    {
        CleanupResponseBuffer();
        receiverBufferBlock_.Post((bytes, DateTime.UtcNow));
    }

    private void CleanupResponseBuffer(bool force = false)
    {
        while (receiverBufferBlock_.TryReceive(
                   x => force || ((DateTime.UtcNow - x.ReceivedTimeStamp) > ReceiverBufferItemTimeout), 
                   out var response))
        {
            dvrIpPacket_.TryGetCommandId(response.DataBytes, out var cmdId);
            dvrIpPacket_.TryGetSeq(response.DataBytes, out var seq);
            logger_.LogWarning($"Response discarded: dataLength={
                response.DataBytes.Length}, cmdId={
                    cmdId}, seq={
                        seq}, receivedAt: {
                            response.ReceivedTimeStamp}");
        }
    }

    private static string CalcXmHash(string password)
    {
        const string hashBase = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        var md5 = MD5.HashData(Encoding.ASCII.GetBytes(password));

        var x1 = (byte)hashBase[(md5[0] + md5[1]) % 62];
        var x2 = (byte)hashBase[(md5[2] + md5[3]) % 62];
        var x3 = (byte)hashBase[(md5[4] + md5[5]) % 62];
        var x4 = (byte)hashBase[(md5[6] + md5[7]) % 62];
        var x5 = (byte)hashBase[(md5[8] + md5[9]) % 62];
        var x6 = (byte)hashBase[(md5[10] + md5[11]) % 62];
        var x7 = (byte)hashBase[(md5[12] + md5[13]) % 62];
        var x8 = (byte)hashBase[(md5[14] + md5[15]) % 62];

        var str = Encoding.ASCII.GetString(new[]
        {
            x1, x2, x3, x4, x5, x6, x7, x8
        });

        return str;
    }
}