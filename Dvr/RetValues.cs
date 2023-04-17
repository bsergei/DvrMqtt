﻿namespace Dvr;

public static class RetValues
{
    public const int RetOk = 100;

    public static readonly IDictionary<int, (bool Success, string Message)> ErrorLookup =
        new Dictionary<int, (bool, string)>
        {
            { 100, (true, "OK") },
            { 101, (false, "Unknown error") },
            { 102, (false, "Invalid version") },
            { 103, (false, "Invalid request") },
            { 104, (false, "Already logged in") },
            { 105, (false, "Not logged in") },
            { 106, (false, "Wrong username or password") },
            { 107, (false, "Access denied") },
            { 108, (false, "Timed out") },
            { 109, (false, "File not found") },
            { 110, (true, "Complete search results") },
            { 111, (true, "Partial search results") },
            { 112, (false, "User already exists") },
            { 113, (false, "User does not exist") },
            { 114, (false, "Group already exists") },
            { 115, (false, "Group does not exist") },
            { 117, (false, "Invalid message") },
            { 118, (false, "PTZ protocol not set") },
            { 119, (true, "No search results") },
            { 120, (false, "Disabled") },
            { 121, (false, "Channel not connected") },
            { 150, (true, "Reboot required") },
            { 202, (false, "FIXME Error 202") },
            { 203, (false, "Wrong password") },
            { 204, (false, "Wrong username") },
            { 205, (false, "Locked out") },
            { 206, (false, "Banned") },
            { 207, (false, "Already logged in") },
            { 208, (false, "Illegal value") },
            { 209, (false, "FIXME Error 209") },
            { 210, (false, "FIXME Error 210") },
            { 211, (false, "Object does not exist") },
            { 212, (false, "Account in use") },
            { 213, (false, "Subset larger than superset") },
            { 214, (false, "Illegal characters in password") },
            { 215, (false, "Passwords do not match") },
            { 216, (false, "Username reserved") },
            { 502, (false, "Illegal command") },
            { 503, (true, "Intercom turned on") },
            { 504, (true, "Intercom turned off") },
            { 511, (true, "Upgrade started") },
            { 512, (false, "Upgrade not started") },
            { 513, (false, "Invalid upgrade data") },
            { 514, (true, "Upgrade successful") },
            { 515, (false, "Upgrade failed") },
            { 521, (false, "Reset failed") },
            { 522, (true, "Reset successful--reboot required") },
            { 523, (false, "Reset data invalid") },
            { 602, (true, "Import successful--restart required") },
            { 603, (true, "Import successful--reboot required") },
            { 604, (false, "Configuration write failed") },
            { 605, (false, "Unsupported feature in configuration") },
            { 606, (false, "Configuration read failed") },
            { 607, (false, "Configuration not found") },
            { 608, (false, "Illegal configuration syntax") },
        };
}