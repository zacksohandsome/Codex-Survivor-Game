using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Survivor.Editor
{
    // Uses the already-authorized official Package Manager API; never blocks the Editor update loop.
    public static class M5RecorderSetup
    {
        private static SearchRequest search;
        private static AddRequest add;
        public static string Status { get; private set; } = "idle";
        public static string Version { get; private set; }
        public static void Search()
        { search = Client.Search("com.unity.recorder"); Status = "searching"; EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (search == null || !search.IsCompleted) return;
            EditorApplication.update -= Poll;
            if (search.Status != StatusCode.Success || search.Result.Length == 0) { Status = "search_failed"; return; }
            Version = search.Result[0].versions.latestCompatible; Status = "found";
        }
        public static void Install()
        {
            if (Status != "found" || string.IsNullOrEmpty(Version)) return;
            add = Client.Add("com.unity.recorder@" + Version); Status = "installing"; EditorApplication.update += PollAdd;
        }
        private static void PollAdd()
        {
            if (add == null || !add.IsCompleted) return;
            EditorApplication.update -= PollAdd;
            Status = add.Status == StatusCode.Success ? "installed" : "install_failed";
        }
    }
}
