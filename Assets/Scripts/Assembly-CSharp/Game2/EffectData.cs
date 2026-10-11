using System;

namespace Game2
{
	public class EffectData
	{
		public Image img;

		public ImageInfo[] imgInfo;

		public Frame[] frame;

		public short[] arrFrame;

		public short[][] anim_data = new short[16][];

		public int ID;

		public int typeData;

		public int width;

		public int height;

		public ImageInfo getImageInfo(sbyte id)
		{
			if (imgInfo == null) return null;
            for (int i = 0; i < imgInfo.Length; i++)
			{
				if (imgInfo[i].ID == (id & 255))
				{
					return imgInfo[i];
				}
			}
			return null;
		}

		public short[] get()
		{
			return arrFrame;
		}

		public short[] get(int index)
		{
			if (index < 0 || index >= anim_data.Length)
			{
				index = 0;
			}
			if (anim_data[index] == null)
			{
				return new short[1];
			}
			return anim_data[index];
		}

		public void readData(string patch)
		{
			DataInputStream dataInputStream = null;
			try
			{
				dataInputStream = MyStream.readFile(patch);
			}
			catch (Exception)
			{
				return;
			}
			if (dataInputStream != null) readData(dataInputStream.r);
		}

		public void readData2(string patch)
		{
			DataInputStream dataInputStream = null;
			try
			{
				dataInputStream = MyStream.readFile(patch);
			}
			catch (Exception)
			{
				return;
			}
			if (dataInputStream != null) readEffect(dataInputStream.r);
		}

		public void readEffect(myReader msg)
		{
            ReadCheckedEffect(msg, false);
		}

		public void readData(myReader iss)
		{
            ReadCheckedEffect(iss, false);
		}

        // Parse into temporary arrays: malformed packets must never replace a working effect.
        private sealed class EffectReader
        {
            readonly sbyte[] bytes; int position;
            public EffectReader(sbyte[] data) { bytes = data; }
            public int Remaining { get { return bytes.Length - position; } }
            public int Byte() { if (Remaining < 1) throw new System.IO.InvalidDataException("Truncated effect"); return bytes[position++] & 255; }
            public short Short() { if (Remaining < 2) throw new System.IO.InvalidDataException("Truncated effect"); return (short)((Byte() << 8) | Byte()); }
            public int Count(int minimumBytes) { int n = Short(); if (n < 0 || n > Remaining / minimumBytes) throw new System.IO.InvalidDataException("Invalid effect count"); return n; }
        }
        private bool TryReadEffect(sbyte[] bytes, bool wide)
        {
            if (bytes == null || bytes.Length == 0) return false;
            try {
                var r = new EffectReader(bytes);
                int count = r.Byte();
                if (count == 0 || count > 127 || count > r.Remaining / (wide ? 7 : 5)) return false;
                var images = new ImageInfo[count];
                for (int i = 0; i < count; i++) {
                    images[i] = new ImageInfo { ID = r.Byte(), x0 = wide ? r.Short() : r.Byte(), y0 = wide ? r.Short() : r.Byte(), w = r.Byte(), h = r.Byte() };
                    if (images[i].x0 < 0 || images[i].y0 < 0 || images[i].w <= 0 || images[i].h <= 0) return false;
                }
                int frameCount = r.Count(1);
                if (frameCount == 0) return false;
                var frames = new Frame[frameCount];
                int left = 0, top = 0, right = 0, bottom = 0;
                for (int j = 0; j < frameCount; j++) {
                    int parts = r.Byte(); if (parts > 127 || parts > r.Remaining / 5) return false;
                    var f = new Frame { dx = new short[parts], dy = new short[parts], idImg = new sbyte[parts] };
                    frames[j] = f;
                    for (int k = 0; k < parts; k++) {
                        f.dx[k] = r.Short(); f.dy[k] = r.Short(); int sprite = r.Byte(); f.idImg[k] = unchecked((sbyte)sprite);
                        ImageInfo info = null;
                        for (int i = 0; i < count; i++) if (images[i].ID == sprite) { info = images[i]; break; }
                        if (info == null) return false;
                        if (j == 0) { left = System.Math.Min(left, f.dx[k]); top = System.Math.Min(top, f.dy[k]); right = System.Math.Max(right, f.dx[k] + info.w); bottom = System.Math.Max(bottom, f.dy[k] + info.h); }
                    }
                }
                int sequenceCount = r.Count(2);
                var sequence = new short[sequenceCount]; var animations = new short[16][];
                var segment = new System.Collections.Generic.List<short>(); bool markers = false;
                for (int i = 0; i < sequenceCount; i++) {
                    short value = r.Short(); sequence[i] = value;
                    if (ID >= 201 && value < -500) {
                        int index = -(value + 500); if (index < 0 || index >= animations.Length) return false;
                        animations[index] = segment.ToArray(); segment.Clear(); markers = true;
                    } else { if (value < 0 || value >= frameCount) return false; segment.Add(value); }
                }
                // Legacy assets include optional exporter trailers after the declared sequence.
                if (!markers) animations[0] = segment.ToArray();
                else if (segment.Count > 0) animations[0] = segment.ToArray();
                short[] fallback = animations[2] ?? animations[0] ?? new short[1];
                for (int i = 0; i < animations.Length; i++) if (animations[i] == null) animations[i] = fallback;
                imgInfo = images; frame = frames; arrFrame = sequence; anim_data = animations;
                width = right - left; height = bottom - top; return true;
            } catch (System.IO.InvalidDataException) { return false; }
        }
        private void ReadCheckedEffect(myReader source, bool wideFirst)
        {
            if (source == null || source.buffer == null) return;
            var bytes = new sbyte[source.available()]; source.read(ref bytes, 0, bytes.Length);
            if (!TryReadEffect(bytes, wideFirst)) TryReadEffect(bytes, !wideFirst);
        }

		public void readData(sbyte[] data)
		{
			myReader iss = new myReader(data);
			readData(iss);
		}

		public void readDataNewBoss(sbyte[] data, sbyte typeread)
		{
			myReader msg = new myReader(data);
			readMobNew(msg, typeread);
		}

		public void paintFrame(mGraphics g, int f, int x, int y, int trans, int layer)
		{
			if (this.frame == null || f < 0 || f >= this.frame.Length || img == null)
			{
				return;
			}
			Frame frame = this.frame[f];
			for (int i = 0; i < frame.dx.Length; i++)
			{
				ImageInfo imageInfo = getImageInfo(frame.idImg[i]);
                if (imageInfo == null) continue;
				try
				{
					switch (trans)
					{
					case -1:
						g.drawRegion(img, imageInfo.x0, imageInfo.y0, imageInfo.w, imageInfo.h, 0, x + frame.dx[i], y + frame.dy[i], 0);
						break;
					case 0:
						g.drawRegion(img, imageInfo.x0, imageInfo.y0, imageInfo.w, imageInfo.h, 0, x + frame.dx[i], y + frame.dy[i] - ((layer < 4 && layer > 0) ? GameCanvas.transY : 0), 0);
						break;
					case 1:
						g.drawRegion(img, imageInfo.x0, imageInfo.y0, imageInfo.w, imageInfo.h, 2, x - frame.dx[i], y + frame.dy[i] - ((layer < 4 && layer > 0) ? GameCanvas.transY : 0), StaticObj.TOP_RIGHT);
						break;
					case 2:
						g.drawRegion(img, imageInfo.x0, imageInfo.y0, imageInfo.w, imageInfo.h, 7, x - frame.dx[i], y + frame.dy[i] - ((layer < 4 && layer > 0) ? GameCanvas.transY : 0), StaticObj.VCENTER_HCENTER);
						break;
					}
				}
				catch (Exception)
				{
				}
			}
		}

		public void readMobNew(myReader msg, sbyte typeread)
		{
            ReadCheckedEffect(msg, typeread != 1);
		}
	}
}
