#if UNITY_EDITOR
using System;
using System.Reflection;

namespace Spirit604.Extensions
{
    public static class ConsoleExtension
    {
        [Flags]
        public enum LogModes
        {
            /// <summary> All user-generated log entries. </summary>
            UserAll = 0,

            /// <summary> Editor-generated errors. </summary>
            EditorErrors = 1 << 0,

            /// <summary> Internal warnings. </summary>
            EditorWarnings = 1 << 7,

            /// <summary> User-generated red log entries (errors, exceptions, assertions). </summary>
            UserErrorsAndExceptions = 1 << 8,

            /// <summary> User and editor-generated warnings. </summary>
            UserAndEditorWarnings = 1 << 9,

            /// <summary> User and editor-generated info messages. </summary>
            UserAndEditorInfos = 1 << 10,

            /// <summary> Mode of the "No script asset for..." warning. </summary>
            NoScriptAssetWarning = 1 << 18,

            /// <summary> User-generated info messages. </summary>
            UserInfo = UserAndEditorInfos | 1 << 14 | 1 << 23, // it's unknown what 14 and 23 are exactly for.

            /// <summary> User-generated warnings. </summary>
            UserWarning = UserAndEditorWarnings | 1 << 14 | 1 << 23
        }

        public static void ClearErrorsAndWarnings()
        {
            MethodInfo removeLogEntriesByModeMethod = GetRemoveLogEntriesByMode();

            RemoveLogEntriesByMode(removeLogEntriesByModeMethod, LogModes.UserErrorsAndExceptions);
            RemoveLogEntriesByMode(removeLogEntriesByModeMethod, LogModes.EditorErrors);
            RemoveLogEntriesByMode(removeLogEntriesByModeMethod, LogModes.UserAndEditorWarnings);
            RemoveLogEntriesByMode(removeLogEntriesByModeMethod, LogModes.EditorWarnings);
        }

        public static void ClearAll()
        {
            MethodInfo removeLogEntriesByModeMethod = GetRemoveLogEntriesByMode();

            RemoveLogEntriesByMode(removeLogEntriesByModeMethod, LogModes.UserAll);
        }

        private static MethodInfo GetRemoveLogEntriesByMode()
        {
            var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            var logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor.dll");

            var removeLogEntriesByModeMethod = logEntryType.GetMethod("RemoveLogEntriesByMode", BindingFlags.NonPublic | BindingFlags.Static);
            return removeLogEntriesByModeMethod;
        }

        private static void RemoveLogEntriesByMode(MethodInfo removeLogEntriesByModeMethod, LogModes mode)
        {
            removeLogEntriesByModeMethod?.Invoke(null, new object[] { (int)mode });
        }
    }
}
#endif