using System;
using System.Net.NetworkInformation;
using System.Threading;
using UnityEngine;

namespace Game1
{
	public class Main : MonoBehaviour
	{
        private void Awake()
        {
            if (main != null)
            {
                Management.tab = TabType.Tab1;
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
        }
        public static Main main;

		public static mGraphics g;

		public static GameMidlet midlet;

		public static string res = "res";

		public static string mainThreadName;

		public static bool started;

		public static bool isIpod;

		public static bool isIphone4;

		public static bool isPC;

		public static bool isWindowsPhone;

		public static bool isIPhone;

		public static bool IphoneVersionApp;

		public static string IMEI;

		public static int versionIp;

		public static int numberQuit = 1;

		public static int typeClient = 4;

		public const sbyte PC_VERSION = 4;

		public const sbyte IP_APPSTORE = 5;

		public const sbyte WINDOWSPHONE = 6;

		private int level;

		public const sbyte IP_JB = 3;

		private int updateCount;

		private int paintCount;

		private int count;

		private int fps;

		private int max;

		private int up;

		private int upmax;

		private long timefps;

		private long timeup;

		private bool isRun;

		public static int waitTick;

		public static int f;

		public static bool isResume;

		public static bool isMiniApp = true;

		public static bool isQuitApp;

		private Vector2 lastMousePos;

		public static int a = 1;

		public static bool isCompactDevice = true;

		private void Start()
		{
			if (started)
			{
				return;
			}
			#if UNITY_WEBGL && !UNITY_EDITOR
            mainThreadName = "Main";
            isIPhone = IphoneVersionApp = NroWeb.BrowserPlatform.IsMobile;
            isPC = !isIPhone;
            GameCanvas.isTouch = isIPhone;
#else
            if (Thread.CurrentThread.Name != "Main")
			{
				Thread.CurrentThread.Name = "Main";
			}
			mainThreadName = Thread.CurrentThread.Name;
			isPC = Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer;
			isIPhone = (IphoneVersionApp = Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android);
			if (isIPhone)
			{
				isPC = false;
			}
			if (isIPhone)
			{
				isPC = false;
			}
			#endif
            started = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("NRO_WEB_DEVICE Game1 pc=" + isPC + " mobile=" + isIPhone);
#endif
			if (isPC && !isIPhone)
			{
				level = Rms.loadRMSInt("levelScreenKN");
				if (level == 1)
				{
					if (!NroWeb.BrowserPlatform.IsBrowser) Screen.SetResolution(720, 320, fullscreen: false);
				}
				else
				{
					if (!NroWeb.BrowserPlatform.IsBrowser) Screen.SetResolution(1024, 600, fullscreen: false);
				}
			}
			else if (isIPhone)
			{
				if (!NroWeb.BrowserPlatform.IsBrowser) Screen.fullScreen = true;
				GameCanvas.isTouch = true;
			}
			ModFunc.GI().LoadGame();
		}

		private void SetInit()
		{
			base.enabled = true;
		}

		private void OnHideUnity(bool isGameShown)
		{
			if (!isGameShown)
			{
				Time.timeScale = 0f;
			}
			else
			{
				Time.timeScale = 1f;
			}
		}

		private void OnGUI()
		{
			if (count < 10)
			{
				return;
			}
			if (fps == 0)
			{
				timefps = mSystem.currentTimeMillis();
			}
			else if (mSystem.currentTimeMillis() - timefps > 1000)
			{
				max = fps;
				fps = 0;
				timefps = mSystem.currentTimeMillis();
			}
			fps++;
			checkInput();
			#if !UNITY_WEBGL || UNITY_EDITOR
Session_ME.update();
Session_ME2.update();
#endif
			if (Management.tab == TabType.Tab1 && Event.current.type.Equals(EventType.Repaint) && (NroWeb.BrowserPlatform.IsBrowser || paintCount <= updateCount))
			{
				if (GameMidlet.gameCanvas != null)
				{
					GameMidlet.gameCanvas.paint(g);
				}
				paintCount++;
				if (g != null)
				{
					g.reset();
				}
			}
		}

		public void setsizeChange()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (isRun && (ScaleGUI.WIDTH != Screen.width || ScaleGUI.HEIGHT != Screen.height)) {
                ScaleGUI.initScaleGUI();
                GameCanvas.w = MotherCanvas.instance.getWidthz();
                GameCanvas.h = MotherCanvas.instance.getHeightz();
                GameCanvas.hw = GameCanvas.w / 2;
                GameCanvas.hh = GameCanvas.h / 2;
                GameScr.RefreshBrowserControls();
                if (GameCanvas.currentScreen is GameScr && TileMap.tmw > 0) GameScr.loadCamera(false, -1, -1);
                if (GameCanvas.currentScreen is LoginScr) ((LoginScr)GameCanvas.currentScreen).ResizeBrowser();
                if (GameCanvas.currentScreen is ServerScr) ((ServerScr)GameCanvas.currentScreen).ResizeBrowser();
                if (GameCanvas.currentScreen is ServerListScreen) ((ServerListScreen)GameCanvas.currentScreen).ResizeBrowser();
            }
#endif
			if (!isRun)
			{
				Screen.orientation = ScreenOrientation.AutoRotation;
				Application.runInBackground = true;
				base.useGUILayout = false;
				isCompactDevice = detectCompactDevice();
				if (main == null)
				{
					main = this;
				}
				isRun = true;
				ScaleGUI.initScaleGUI();
				if (isPC)
				{
					IMEI = SystemInfo.deviceUniqueIdentifier;
				}
				else
				{
					IMEI = NroWeb.BrowserPlatform.IsBrowser ? "WEB-" + SystemInfo.deviceUniqueIdentifier : GetMacAddress();
				}
				if (isPC && !isIPhone)
				{
					if (!NroWeb.BrowserPlatform.IsBrowser) Screen.fullScreen = false;
				}
				if (isIPhone && !isPC)
				{
					if (!NroWeb.BrowserPlatform.IsBrowser) Screen.fullScreen = true;
				}
				if (isPC)
				{
					typeClient = 4;
				}
				if (isWindowsPhone)
				{
					typeClient = 6;
				}
				if (isIPhone || IphoneVersionApp)
				{
					typeClient = 4;
				}
				if (iPhoneSettings.generation == iPhoneGeneration.iPodTouch4Gen)
				{
					isIpod = true;
				}
				if (iPhoneSettings.generation == iPhoneGeneration.iPhone4)
				{
					isIphone4 = true;
				}
				g = new mGraphics();
				midlet = new GameMidlet();
				TileMap.loadBg();
				Paint.loadbg();
				PopUp.loadBg();
				GameScr.loadBg();
				InfoMe.gI().loadCharId();
				Panel.loadBg();
				Menu.loadBg();
				Key.mapKeyPC();
				SoundMn.gI().loadSound(TileMap.mapID);
				g.CreateLineMaterial();
			}
		}

		public static void setBackupIcloud(string path)
		{
		}

		public string GetMacAddress() {
#if UNITY_WEBGL && !UNITY_EDITOR
return "WEB-" + SystemInfo.deviceUniqueIdentifier;
#else

			_ = string.Empty;
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			for (int i = 0; i < allNetworkInterfaces.Length; i++)
			{
				PhysicalAddress physicalAddress = allNetworkInterfaces[i].GetPhysicalAddress();
				if (physicalAddress.ToString() != string.Empty)
				{
					return physicalAddress.ToString();
				}
			}
			return string.Empty;
		
#endif
}

		public void doClearRMS()
		{
			if (isPC && Rms.loadRMSInt("lastZoomlevel") != mGraphics.zoomLevel)
			{
				Rms.saveRMSInt("lastZoomlevel", mGraphics.zoomLevel);
				Rms.saveRMSInt("levelScreenKN", level);
			}
		}

		public static void closeKeyBoard()
		{
			if (TouchScreenKeyboard.visible)
			{
				TField.kb.active = false;
				TField.kb = null;
			}
		}

		
		private void FixedUpdate()
		{

			Rms.update();
			count++;
			if (count >= 10)
			{
				if (up == 0)
				{
					timeup = mSystem.currentTimeMillis();
				}
				else if (mSystem.currentTimeMillis() - timeup > 1000)
				{
					upmax = up;
					up = 0;
					timeup = mSystem.currentTimeMillis();
				}
				up++;
				setsizeChange();
				updateCount++;
				ipKeyboard.update();
				if (GameMidlet.gameCanvas != null)
				{
					GameMidlet.gameCanvas.update();
				}
#if UNITY_WEBGL && !UNITY_EDITOR
                Session_ME.update(); Session_ME2.update();
#endif
				Image.update();
				DataInputStream.update();
				f++;
				if (f > 8)
				{
					f = 0;
				}
				if (!isPC)
				{
					_ = 1 / a;
				}
			}
		}

        internal void checkInput()
        {
            if (!Management.CanProcessInput(TabType.Tab1)) return;
			if (isPC && !ModFunc.isOpenAccMAnager && Event.current.type == EventType.KeyDown
				&& Event.current.keyCode == KeyCode.V && Event.current.control)
			{
				try { GameMidlet.gameCanvas.pasteClipboard(); }
				catch (System.Exception ex) { Debug.LogWarning("Clipboard paste unavailable: " + ex.GetType().Name); }
				Event.current.Use();
				return;
			}
            if (Input.GetMouseButtonDown(0))
			{
				Vector3 mousePosition = Input.mousePosition;
				GameMidlet.gameCanvas.pointerPressed((int)(mousePosition.x / (float)mGraphics.zoomLevel), (int)(((float)Screen.height - mousePosition.y) / (float)mGraphics.zoomLevel) + mGraphics.addYWhenOpenKeyBoard);
				lastMousePos.x = mousePosition.x / (float)mGraphics.zoomLevel;
				lastMousePos.y = mousePosition.y / (float)mGraphics.zoomLevel + (float)mGraphics.addYWhenOpenKeyBoard;
			}
			if (Input.GetMouseButton(0))
			{
				Vector3 mousePosition2 = Input.mousePosition;
				GameMidlet.gameCanvas.pointerDragged((int)(mousePosition2.x / (float)mGraphics.zoomLevel), (int)(((float)Screen.height - mousePosition2.y) / (float)mGraphics.zoomLevel) + mGraphics.addYWhenOpenKeyBoard);
				lastMousePos.x = mousePosition2.x / (float)mGraphics.zoomLevel;
				lastMousePos.y = mousePosition2.y / (float)mGraphics.zoomLevel + (float)mGraphics.addYWhenOpenKeyBoard;
			}
			if (Input.GetMouseButtonUp(0))
			{
				Vector3 mousePosition3 = Input.mousePosition;
				lastMousePos.x = mousePosition3.x / (float)mGraphics.zoomLevel;
				lastMousePos.y = mousePosition3.y / (float)mGraphics.zoomLevel + (float)mGraphics.addYWhenOpenKeyBoard;
				GameMidlet.gameCanvas.pointerReleased((int)(mousePosition3.x / (float)mGraphics.zoomLevel), (int)(((float)Screen.height - mousePosition3.y) / (float)mGraphics.zoomLevel) + mGraphics.addYWhenOpenKeyBoard);
			}
			if (Input.anyKeyDown && Event.current.type == EventType.KeyDown)
			{
				int num = MyKeyMap.map(Event.current.keyCode);
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					switch (Event.current.keyCode)
					{
					case KeyCode.Alpha2:
						num = 64;
						break;
					case KeyCode.Minus:
						num = 95;
						break;
					}
				}
				if (num != 0)
				{
					GameMidlet.gameCanvas.keyPressedz(num);
				}
			}
			if (Event.current.type == EventType.KeyUp)
			{
				int num2 = MyKeyMap.map(Event.current.keyCode);
				if (num2 != 0)
				{
					GameMidlet.gameCanvas.keyReleasedz(num2);
				}
			}
			if (isPC)
			{
				GameMidlet.gameCanvas.scrollMouse((int)(Input.GetAxis("Mouse ScrollWheel") * 10f));
				int num3 = (int)Input.mousePosition.x;
				float y = Input.mousePosition.y;
				int x = num3 / mGraphics.zoomLevel;
				int y2 = (Screen.height - (int)y) / mGraphics.zoomLevel;
				GameMidlet.gameCanvas.pointerMouse(x, y2);
			}
		}

		private void OnApplicationQuit()
		{
			GameCanvas.bRun = false;
			Session_ME.gI().close();
			Session_ME2.gI().close();
			if (isPC)
			{
				Application.Quit();
			}
		}

		private void OnApplicationPause(bool paused)
		{
			isResume = false;
			if (paused)
			{
				if (GameCanvas.isWaiting())
				{
					isQuitApp = true;
				}
			}
			else
			{
				isResume = true;
			}
			if (TouchScreenKeyboard.visible)
			{
				TField.kb.active = false;
				TField.kb = null;
			}
			if (isQuitApp)
			{
				Application.Quit();
			}
		}

		public static void exit()
		{
			if (isPC)
			{
				main.OnApplicationQuit();
			}
			else
			{
				a = 0;
			}
		}

		public static bool detectCompactDevice()
		{
			if (iPhoneSettings.generation != iPhoneGeneration.iPhone && iPhoneSettings.generation != iPhoneGeneration.iPhone3G && iPhoneSettings.generation != iPhoneGeneration.iPodTouch1Gen)
			{
				return iPhoneSettings.generation != iPhoneGeneration.iPodTouch2Gen;
			}
			return false;
		}

		public static bool checkCanSendSMS()
		{
			if (iPhoneSettings.generation != iPhoneGeneration.iPhone3GS && iPhoneSettings.generation != iPhoneGeneration.iPhone4)
			{
				return iPhoneSettings.generation > iPhoneGeneration.iPodTouch4Gen;
			}
			return true;
		}
	}
}
