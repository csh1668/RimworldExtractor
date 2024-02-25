using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimworldExtractorInternal
{
    public static class Log
    {
        public static TextWriter Out { private get; set; } = Console.Out;

        public static IEnumerable<string> Messages => _logQueue;

        public const string Separator = "::";
        public const string PrefixError = "에러";
        public const string PrefixWarning = "경고";
        public const string PrefixMessage = "메시지";

        private static readonly Queue<string> _logQueue = new();
        private static readonly HashSet<int> _hashes = new();
        private const int MAX_COUNT = 999;

        public static void Err(string message)
        {
            var str = $"{PrefixError}{Separator}{GetCallStack()}{Separator}{message}";
            Out.WriteLine(str);
            StoreMessage(str);
        }

        public static void ErrOnce(string message, int hash)
        {
            if (_hashes.Contains(hash))
                return;
            _hashes.Add(hash);
            Err(message);
        }

        public static void Wrn(string message)
        {
            var str = $"{PrefixWarning}{Separator}{GetCallStack()}{Separator}{message}";
            Out.WriteLine(str);
            StoreMessage(message);
        }

        public static void WrnOnce(string message, int hash)
        {
            if (_hashes.Contains(hash))
                return;
            _hashes.Add(hash);
            Wrn(message);
        }

        public static void Msg(string message)
        {
            var str = $"{PrefixMessage}{Separator}{GetCallStack()}{Separator}{message}";
            Out.WriteLine(str);
            StoreMessage(message);
        }

        private static string GetCallStack()
        {
            var curMethod = new StackTrace().GetFrame(2)?.GetMethod();
            if (curMethod?.DeclaringType == typeof(Log))
            {
                curMethod = new StackTrace().GetFrame(3)?.GetMethod();
            }
            return $"{curMethod?.DeclaringType?.Name ?? "UNKNOWN Type"}.{curMethod?.Name ?? "UNKNOWN Method"}()";
        }

        private static void StoreMessage(string message)
        {
            if (_logQueue.Count > MAX_COUNT)
            {
                _logQueue.Dequeue();
            }
            _logQueue.Enqueue(message);
        }
    }
}
