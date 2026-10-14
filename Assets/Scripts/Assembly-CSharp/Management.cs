using UnityEngine.SceneManagement;

public class Management
{
    public static bool isLogo = true;

    public static string IpServer = "NRO NGU:160.191.242.105:14445:0,LOCAL:127.0.0.1:14445:0,0,0";

    public static string LinkWeb = "@ngu";

    private static TabType[] tabTypes = new TabType[]
    {
        TabType.Tab1,
        TabType.Tab2
    };

    private static string[] SceneNames = new string[]
    {
         "NROL",
         "NROL1"
    };
    public static TabType tab;
    private static int lastTabChangeFrame = -1;

    public static bool CanProcessInput(TabType inputTab)
    {
        return tab == inputTab && (!NroWeb.BrowserPlatform.IsBrowser || UnityEngine.Time.frameCount != lastTabChangeFrame);
    }

    private static bool SceneLoad(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    public static void ChangeTab(int index)
    {
        if (index < 0 || index >= tabTypes.Length) return;
        if (tab != tabTypes[index] && NroWeb.BrowserPlatform.IsBrowser)
            lastTabChangeFrame = UnityEngine.Time.frameCount;
        tab = tabTypes[index];
        if (!SceneLoad(SceneNames[index]))
        {
            SceneManager.LoadScene(SceneNames[index], LoadSceneMode.Additive);
        }
        if (NroWeb.BrowserPlatform.IsBrowser) NroWeb.BrowserHost.TabChanged(index);
    }
}

