using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets._CI.Editor
{
    class LogUtility
    {
        private static string create_message(string tag, string message, params object[] args)
        {
            string logmessage = string.Format(message, args);
            return string.Format("{0}> {1}: {2}", tag, DateTime.Now.ToLongTimeString(), logmessage);
        }

        public static void log(string tag, string message, params object[] args)
        {
            Debug.Log(create_message(tag, message, args));
        }

        public static void error(string tag, string message, params object[] args)
        {
            Debug.LogError(create_message(tag, message, args));
        }
    }
}
