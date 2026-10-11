using System;
using System.Collections.Generic;
using Game2.Assets.src.e;

namespace Game2
{
	public class SmallImage
	{
		public static int[][] smallImg;

		public static SmallImage instance;

		public static Image[] imgbig;

		public static Small[] imgNew;

		public static MyVector vKeys = new MyVector();

		public static Image imgEmpty = null;

		public static sbyte[] newSmallVersion;

		public static int smallCount;

		public static short maxSmall;

		public static Dictionary<int, Image> imageRaw = new Dictionary<int, Image>();

        private sealed class PendingIcon
        {
            public int id;
            public long retryAt;
            public int delay = 3000;
        }
        private static readonly Dictionary<int, PendingIcon> pendingIcons = new Dictionary<int, PendingIcon>();
        private static readonly Queue<PendingIcon> retryQueue = new Queue<PendingIcon>();

        private static void SendIconRequest(PendingIcon pending)
        {
            pending.retryAt = mSystem.currentTimeMillis() + pending.delay;
            pending.delay = System.Math.Min(30000, pending.delay * 2);
            Service.gI().requestIcon(pending.id);
        }

        private static void RequestMissingIcon(int id)
        {
            PendingIcon pending;
            if (pendingIcons.TryGetValue(id, out pending)) return;
            pending = new PendingIcon { id = id };
            pendingIcons[id] = pending;
            retryQueue.Enqueue(pending);
            SendIconRequest(pending);
        }

        private static void RetryMissingIcons()
        {
            if (!Session_ME.gI().isConnected() && !Session_ME2.gI().isConnected()) return;
            int budget = System.Math.Min(4, retryQueue.Count);
            long now = mSystem.currentTimeMillis();
            while (budget-- > 0) {
                PendingIcon pending = retryQueue.Dequeue(), current;
                if (!pendingIcons.TryGetValue(pending.id, out current) || !ReferenceEquals(current, pending)) continue;
                if (now >= pending.retryAt) SendIconRequest(pending);
                retryQueue.Enqueue(pending);
            }
        }

		public SmallImage()
		{
			readImage();
		}

		public static void loadBigRMS()
		{
			if (imgbig == null)
			{
				imgbig = new Image[5]
				{
					GameCanvas.loadImageRMS("/img/Big0.png"),
					GameCanvas.loadImageRMS("/img/Big1.png"),
					GameCanvas.loadImageRMS("/img/Big2.png"),
					GameCanvas.loadImageRMS("/img/Big3.png"),
					GameCanvas.loadImageRMS("/img/Big4.png")
				};
			}
		}

		public static void loadBigImage()
		{
			imgEmpty = Image.createRGBImage(new int[1], 1, 1, bl: true);
		}

		public static void init()
		{
			instance = null;
			instance = new SmallImage();
		}

		public void readImage()
		{
			int num = 0;
			try
			{
				DataInputStream dataInputStream = new DataInputStream(Rms.loadRMS("NR_image"));
				short num2 = dataInputStream.readShort();
				smallImg = new int[num2][];
				for (int i = 0; i < smallImg.Length; i++)
				{
					smallImg[i] = new int[5];
				}
				for (int j = 0; j < num2; j++)
				{
					num++;
					smallImg[j][0] = dataInputStream.readUnsignedByte();
					smallImg[j][1] = dataInputStream.readShort();
					smallImg[j][2] = dataInputStream.readShort();
					smallImg[j][3] = dataInputStream.readShort();
					smallImg[j][4] = dataInputStream.readShort();
				}
			}
			catch (Exception ex)
			{
				Cout.LogError3("Loi readImage: " + ex.ToString() + "i= " + num);
			}
		}

		public static void clearHastable()
		{
		}

        private static bool EnsureSlot(int id)
        {
            if (id < 0 || id >= short.MaxValue) return false;
            if (imgNew == null) imgNew = new Small[System.Math.Max(32, id + 1)];
            else if (id >= imgNew.Length) Array.Resize(ref imgNew, System.Math.Min(short.MaxValue, System.Math.Max(id + 1, imgNew.Length * 2)));
            return true;
        }

        private static bool HasAtlas(int id)
        {
            if (imgbig == null || smallImg == null || id < 0 || id >= smallImg.Length) return false;
            int[] row = smallImg[id];
            if (row == null || row.Length < 5 || row[0] < 0 || row[0] >= imgbig.Length || imgbig[row[0]] == null) return false;
            for (int i = 1; i < 5; i++) if (row[i] < 0 || row[i] >= 256) return false;
            return true;
        }

        public static bool ReceiveImage(int id, Image image)
        {
            if (!EnsureSlot(id)) return false;
            if (image == null) {
                // A late failed response must not erase an image that already arrived.
                if (imgNew[id] != null && imgNew[id].img != null && imgNew[id].img != imgEmpty) return false;
                imageRaw.Remove(id);
                if (imgEmpty == null) imgEmpty = Image.createRGBImage(new int[1], 1, 1, bl: true);
                imgNew[id] = new Small(imgEmpty, id);
                RequestMissingIcon(id);
                return false;
            }
            pendingIcons.Remove(id);
            imgNew[id] = new Small(image, id);
            if (mGraphics.zoomLevel > 1) imageRaw[id] = image;
            return true;
        }

		public static void createImage(int id)
		{
            if (!EnsureSlot(id)) return;
            if (imgEmpty == null) imgEmpty = Image.createRGBImage(new int[1], 1, 1, bl: true);
			if (mGraphics.zoomLevel == 1)
			{
				Image image = GameCanvas.loadImage("/SmallImage/Small" + id + ".png");
				if (image != null)
				{
					ReceiveImage(id, image);
					return;
				}
				imgNew[id] = new Small(imgEmpty, id);
				RequestMissingIcon(id);
				return;
			}
			Image image2 = GameCanvas.loadImage("/SmallImage/Small" + id + ".png");
			if (image2 != null)
			{
				ReceiveImage(id, image2);
				return;
			}
			bool flag = false;
			if (imageRaw.ContainsKey(id))
			{
				Image value = null;
				imageRaw.TryGetValue(id, out value);
				if (value != null)
				{
					ReceiveImage(id, value);
				}
				else
				{
					flag = true;
				}
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				imgNew[id] = new Small(imgEmpty, id);
				RequestMissingIcon(id);
			}
		}

		public static void drawSmallImage(mGraphics g, int id, int x, int y, int transform, int anchor)
		{
            if (!EnsureSlot(id)) return;
			if (imgbig != null)
			{
				if (smallImg != null)
				{
					if (!HasAtlas(id))
					{
						Small small = imgNew[id];
						if (small == null)
						{
							createImage(id);
						}
						else
						{
							small.paint(g, transform, x, y, anchor);
						}
					}
					else if (imgbig[smallImg[id][0]] != null)
					{
						g.drawRegion(imgbig[smallImg[id][0]], smallImg[id][1], smallImg[id][2], smallImg[id][3], smallImg[id][4], transform, x, y, anchor);
					}
				}
				else if (GameCanvas.currentScreen != GameScr.gI())
				{
					Small small2 = imgNew[id];
					if (small2 == null)
					{
						createImage(id);
					}
					else
					{
						small2.paint(g, transform, x, y, anchor);
					}
				}
			}
			else
			{
				Small small3 = imgNew[id];
				if (small3 == null)
				{
					createImage(id);
				}
				else
				{
					g.drawRegion(small3, 0, 0, mGraphics.getImageWidth(small3.img), mGraphics.getImageHeight(small3.img), transform, x, y, anchor);
				}
			}
		}

		public static void drawSmallImage(mGraphics g, int id, int f, int x, int y, int w, int h, int transform, int anchor)
		{
            if (!EnsureSlot(id)) return;
			if (imgbig == null)
			{
				Small small = imgNew[id];
				if (small == null)
				{
					createImage(id);
				}
				else
				{
					g.drawRegion(small.img, 0, f * w, w, h, transform, x, y, anchor);
				}
			}
			else if (smallImg == null)
			{
				if (GameCanvas.currentScreen != GameScr.gI())
				{
					Small small2 = imgNew[id];
					if (small2 == null)
					{
						createImage(id);
					}
					else
					{
						small2.paint(g, transform, f, x, y, w, h, anchor);
					}
				}
			}
			else if (!HasAtlas(id))
			{
				Small small3 = imgNew[id];
				if (small3 == null)
				{
					createImage(id);
				}
				else
				{
					small3.paint(g, transform, f, x, y, w, h, anchor);
				}
			}
			else if (smallImg[id][0] != 4 && imgbig[smallImg[id][0]] != null)
			{
				g.drawRegion(imgbig[smallImg[id][0]], 0, f * w, w, h, transform, x, y, anchor);
			}
			else
			{
				Small small4 = imgNew[id];
				if (small4 == null)
				{
					createImage(id);
				}
				else
				{
					small4.paint(g, transform, f, x, y, w, h, anchor);
				}
			}
		}

		public static void update()
		{
            RetryMissingIcons();
            if (imgNew == null) return;
			int num = 0;
			if (GameCanvas.gameTick % 1000 != 0)
			{
				return;
			}
			for (int i = 0; i < imgNew.Length; i++)
			{
				if (imgNew[i] != null)
				{
					num++;
					imgNew[i].update();
					smallCount++;
				}
			}
			if (num > 200 && GameCanvas.lowGraphic)
			{
				imgNew = new Small[maxSmall];
			}
		}
	}
}
