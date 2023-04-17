using System.Reactive.Linq;

namespace Dvr
{
    public static class Contingency
    {
        public static IObservable<IDvrIpWebService?> Run(Func<IDvrIpWebService> dvrWebIpServiceFactory, int reconnectDelaySeconds = 15)
        {
            return Observable.Create<IDvrIpWebService?>(async (observer) =>
            {
                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;

                var isError = false;
                IDvrIpWebService? dvr = null;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (isError)
                        {
                            // Cooldown.
                            await Task.Delay(
                                TimeSpan.FromSeconds(reconnectDelaySeconds), cancellationToken);
                        }

                        dvr = dvrWebIpServiceFactory();
                        var dvrTask = dvr.Run(cancellationToken);
                        await dvr.WhenConnected();

                        observer.OnNext(dvr);

                        await dvrTask;
                        break; // Normal exit.
                    }
                    catch (OperationCanceledException)
                    {
                        break; // Normal exit.
                    }
                    catch (Exception)
                    {
                        isError = true;

                        if (dvr != null)
                        {
                            observer.OnNext(null);
                            await DisposeAnonymousService(dvr);
                            dvr = null;
                        }
                    }
                }

                observer.OnCompleted();

                return () =>
                {
                    cts.Cancel();
                    cts.Dispose();
                };
            });
        }

        private static async Task DisposeAnonymousService(object obj)
        {
            switch (obj)
            {
                case IAsyncDisposable d:
                    await d.DisposeAsync();
                    break;

                case IDisposable d:
                    d.Dispose();
                    break;
            }
        }
    }
}
