using System;

namespace Game1
{
	public class GamePad
	{
		private int xC;

		private int yC;

		private int xM;

		private int yM;

		private int xMLast;

		private int yMLast;

		private int R;

		private int d;

		private int xTemp;

		private int yTemp;

		private int deltaX;

		private int deltaY;

		private int delta;

		private int angle;

		public int xZone;

		public int yZone;

		public int wZone;

		public int hZone;

		private bool isGamePad;
        private readonly bool[] heldDirections = new bool[4];
        private static readonly UnityEngine.KeyCode[] directionKeys = {
            UnityEngine.KeyCode.UpArrow, UnityEngine.KeyCode.DownArrow,
            UnityEngine.KeyCode.LeftArrow, UnityEngine.KeyCode.RightArrow
        };

		public bool isSmallGamePad;

		public bool isMediumGamePad;

		public bool isLargeGamePad;

		public GamePad()
		{
			R = 28;
			if (GameCanvas.w < 300)
			{
				isSmallGamePad = true;
				isMediumGamePad = false;
				isLargeGamePad = false;
			}
			if (GameCanvas.w >= 300 && GameCanvas.w <= 380)
			{
				isSmallGamePad = false;
				isMediumGamePad = true;
				isLargeGamePad = false;
			}
			if (GameCanvas.w > 380)
			{
				isSmallGamePad = false;
				isMediumGamePad = false;
				isLargeGamePad = true;
			}
			if (!isLargeGamePad)
			{
				xZone = 0;
				wZone = GameCanvas.hw;
				yZone = GameCanvas.hh >> 1;
				hZone = GameCanvas.h - 80;
			}
			else
			{
				xZone = 0;
				wZone = GameCanvas.hw / 4 * 3 - 20;
				yZone = GameCanvas.hh >> 1;
				hZone = GameCanvas.h;
			}
		}

		public void update()
		{
			try
			{
				if (GameScr.isAnalog == 0)
				{
					return;
				}
				if (GameCanvas.isPointerDown && !GameCanvas.isPointerJustRelease)
				{
					xTemp = GameCanvas.pxFirst;
					yTemp = GameCanvas.pyFirst;
					if (xTemp < xZone || xTemp > wZone || yTemp < yZone || yTemp > hZone)
					{
						return;
					}
					if (!isGamePad)
					{
						xC = (xM = xTemp);
						yC = (yM = yTemp);
					}
					isGamePad = true;
					deltaX = GameCanvas.px - xC;
					deltaY = GameCanvas.py - yC;
					delta = Math.pow(deltaX, 2) + Math.pow(deltaY, 2);
					d = Res.sqrt(delta);
					if (Math.abs(deltaX) <= 4 && Math.abs(deltaY) <= 4)
					{
                        resetHold();
						return;
					}
					angle = Res.angle(deltaX, deltaY);
					if (!GameCanvas.isPointerHoldIn(xC - R, yC - R, 2 * R, 2 * R))
					{
						if (d != 0)
						{
							yM = deltaY * R / d;
							xM = deltaX * R / d;
							xM += xC;
							yM += yC;
							if (!Res.inRect(xC - R, yC - R, 2 * R, 2 * R, xM, yM))
							{
								xM = xMLast;
								yM = yMLast;
							}
							else
							{
								xMLast = xM;
								yMLast = yM;
							}
						}
						else
						{
							xM = xMLast;
							yM = yMLast;
						}
					}
					else
					{
						xM = GameCanvas.px;
						yM = GameCanvas.py;
					}
					resetHold();
					if (checkPointerMove(2))
					{
						if ((angle <= 360 && angle >= 340) || (angle >= 0 && angle <= 20))
						{
							holdKey((!Main.isPC) ? 6 : 24);
						}
						else if (angle > 40 && angle < 70)
						{
							holdKey((!Main.isPC) ? 6 : 24);
						}
						else if (angle >= 70 && angle <= 110)
						{
							holdKey((!Main.isPC) ? 8 : 22);
						}
						else if (angle > 110 && angle < 120)
						{
							holdKey((!Main.isPC) ? 4 : 23);
						}
						else if (angle >= 120 && angle <= 200)
						{
							holdKey((!Main.isPC) ? 4 : 23);
						}
						else if (angle > 200 && angle < 250)
						{
							holdKey((!Main.isPC) ? 2 : 21);
							holdKey((!Main.isPC) ? 4 : 23);
						}
						else if (angle >= 250 && angle <= 290)
						{
							holdKey((!Main.isPC) ? 2 : 21);
						}
						else if (angle > 290 && angle < 340)
						{
							holdKey((!Main.isPC) ? 2 : 21);
							holdKey((!Main.isPC) ? 6 : 24);
						}
					}
					else
					{
						resetHold();
					}
				}
				else
				{
					xM = (xC = 45);
					if (!isLargeGamePad)
					{
						yM = (yC = GameCanvas.h - 90);
					}
					else
					{
						yM = (yC = GameCanvas.h - 45);
					}
					isGamePad = false;
					resetHold();
				}
			}
			catch (Exception)
			{
			}
		}

		private bool checkPointerMove(int distance)
		{
			if (GameScr.isAnalog == 0)
			{
				return false;
			}
			if (Char.myCharz().statusMe == 3)
			{
				return true;
			}
			try
			{
				for (int num = 2; num > 0; num--)
				{
					int i = GameCanvas.arrPos[num].x - GameCanvas.arrPos[num - 1].x;
					int i2 = GameCanvas.arrPos[num].y - GameCanvas.arrPos[num - 1].y;
					if (Res.abs(i) > distance && Res.abs(i2) > distance)
					{
						return false;
					}
				}
			}
			catch (Exception)
			{
			}
			return true;
		}

		public void release()
        {
            resetHold();
            isGamePad = false;
        }

        private void holdKey(int key)
        {
            if (NroWeb.BrowserPlatform.IsBrowser && Main.isPC && key >= 21 && key <= 24)
                heldDirections[key - 21] = true;
            GameCanvas.keyHold[key] = true;
            GameCanvas.keyPressed[key] = true;
        }

        private void resetHold()
        {
            if (NroWeb.BrowserPlatform.IsBrowser && Main.isPC) {
                // Release only directions owned by this pad. Physical keys can
                // remain held, including an arrow pressed during the drag.
                for (int i = 0; i < heldDirections.Length; i++) {
                    if (!heldDirections[i]) continue;
                    GameCanvas.keyHold[21 + i] = UnityEngine.Input.GetKey(directionKeys[i]);
                    heldDirections[i] = false;
                }
                return;
            }
            GameCanvas.clearKeyHold();
        }

		public void paint(mGraphics g)
		{
			g.drawImage(GameScr.imgAnalog1, xC, yC, mGraphics.HCENTER | mGraphics.VCENTER);
			g.drawImage(GameScr.imgAnalog2, xM, yM, mGraphics.HCENTER | mGraphics.VCENTER);
		}

		public bool disableCheckDrag()
		{
			if (GameScr.isAnalog != 0)
			{
				return isGamePad;
			}
			return false;
		}

		public bool disableClickMove()
		{
			try
			{
				if (GameScr.isAnalog == 0)
				{
					return false;
				}
				return (GameCanvas.px >= xZone && GameCanvas.px <= wZone && GameCanvas.py >= yZone && GameCanvas.py <= hZone) || GameCanvas.px >= GameCanvas.w - 50;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
