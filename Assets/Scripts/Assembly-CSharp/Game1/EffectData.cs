using System;

namespace Game1
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
			for (int i = 0; i < imgInfo.Length; i++)
			{
				if (imgInfo[i] != null && imgInfo[i].ID == id)
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
			readCompatible(msg, 0, false, false);
		}

		public void readData(myReader iss)
		{
			// Resource mob data may finish immediately after its frame definitions.
			readCompatible(iss, 0, ID >= 201, ID == 0);
		}

		public void readData(sbyte[] data)
		{
			readCompatible(new myReader(data), 0, ID >= 201, ID == 0);
		}

		public void readDataNewBoss(sbyte[] data, sbyte typeread)
		{
			readMobNew(new myReader(data), typeread);
		}

		private void readCompatible(myReader source, int coordinateType, bool animations, bool allowMissingSequence)
		{
			if (source == null || source.buffer == null) return;
			int start = source.buffer.Length - source.available();
			for (int attempt = 0; attempt < ((coordinateType == 0) ? 2 : 1); attempt++)
			{
				// Parse against an independent reader and publish only a complete valid result.
				myReader input = new myReader(source.buffer);
				for (int i = 0; i < start; i++) input.readByte();
				EffectData parsed = new EffectData();
				try
				{
					parsed.readFrames(input, coordinateType == 2 || (coordinateType == 0 && attempt == 1), animations, allowMissingSequence);
				}
				catch (Exception)
				{
					continue;
				}
				int consumed = source.available() - input.available();
				for (int i = 0; i < consumed; i++) source.readByte();
				imgInfo = parsed.imgInfo;
				frame = parsed.frame;
				arrFrame = parsed.arrFrame;
				anim_data = parsed.anim_data;
				width = parsed.width;
				height = parsed.height;
				return;
			}
		}

		private void readFrames(myReader input, bool shortCoordinates, bool animations, bool allowMissingSequence)
		{
			int imageCount = input.readUnsignedByte();
			if (imageCount == 0) throw new Exception("Empty effect images");
			imgInfo = new ImageInfo[imageCount];
			for (int i = 0; i < imageCount; i++)
			{
				ImageInfo info = new ImageInfo();
				info.ID = input.readByte();
				if (getImageInfo((sbyte)info.ID) != null) throw new Exception("Duplicate effect image");
				info.x0 = shortCoordinates ? input.readShort() : input.readUnsignedByte();
				info.y0 = shortCoordinates ? input.readShort() : input.readUnsignedByte();
				info.w = input.readUnsignedByte();
				info.h = input.readUnsignedByte();
				if (info.x0 < 0 || info.y0 < 0) throw new Exception("Invalid effect coordinates");
				imgInfo[i] = info;
			}
			int count = input.readShort();
			if (count <= 0 || count > input.available()) throw new Exception("Invalid effect frame count");
			frame = new Frame[count];
			int left = 0, top = 0, right = 0, bottom = 0;
			for (int i = 0; i < count; i++)
			{
				int parts = input.readUnsignedByte();
				if (parts > input.available() / 5) throw new Exception("Truncated effect frame");
				Frame current = new Frame();
				current.dx = new short[parts];
				current.dy = new short[parts];
				current.idImg = new sbyte[parts];
				for (int j = 0; j < parts; j++)
				{
					current.dx[j] = input.readShort();
					current.dy[j] = input.readShort();
					current.idImg[j] = input.readByte();
					ImageInfo info = getImageInfo(current.idImg[j]);
					if (info == null) throw new Exception("Invalid effect sprite");
					if (i == 0)
					{
						left = System.Math.Min(left, current.dx[j]);
						top = System.Math.Min(top, current.dy[j]);
						right = System.Math.Max(right, current.dx[j] + info.w);
						bottom = System.Math.Max(bottom, current.dy[j] + info.h);
					}
				}
				frame[i] = current;
			}
			width = right - left;
			height = bottom - top;
			if (allowMissingSequence && input.available() == 0) return;
			int sequenceCount = input.readShort();
			if (sequenceCount < 0 || sequenceCount > input.available() / 2) throw new Exception("Truncated effect sequence");
			arrFrame = new short[sequenceCount];
			short[] sequence = new short[sequenceCount];
			int length = 0;
			for (int i = 0; i < sequenceCount; i++)
			{
				short value = input.readShort();
				arrFrame[i] = value;
				if (animations && value < 0)
				{
					int animation = System.Math.Abs(value + 500);
					if (animation >= anim_data.Length) throw new Exception("Invalid effect animation");
					anim_data[animation] = new short[length];
					Array.Copy(sequence, anim_data[animation], length);
					length = 0;
				}
				else
				{
					if (value < 0 || value >= frame.Length) throw new Exception("Invalid effect frame");
					sequence[length++] = value;
				}
			}
			if (animations)
			{
				for (int i = 0; i < anim_data.Length; i++)
					if (anim_data[i] == null) anim_data[i] = anim_data[2];
			}
		}

		public void paintFrame(mGraphics g, int f, int x, int y, int trans, int layer)
		{
			if (img == null || this.frame == null || f < 0 || f >= this.frame.Length)
			{
				return;
			}
			Frame frame = this.frame[f];
			for (int i = 0; i < frame.dx.Length; i++)
			{
				ImageInfo imageInfo = getImageInfo(frame.idImg[i]);
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
			readCompatible(msg, typeread == 1 ? 1 : 2, false, false);
		}
	}
}
