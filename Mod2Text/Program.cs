using System;
using System.Collections;
using System.IO;

namespace Mod2Text
{
	class Program
	{
		private static string inFileName, outFileName;

		private static string songTitle;
		private static int songLength, numCh;

		private static Hashtable mop = new Hashtable();

		public static void Main(string[] args)
		{
			// No file specified, convert all *.mod files in current directory
			if (args.Length < 1)
			{
				string cd = Directory.GetCurrentDirectory();
				foreach (string fileName in Directory.GetFiles(cd, "*.mod"))
				{
					convertFile(fileName, fileName + ".txt");
				}
			}
			// or convert the specified file only
			else
			{
				inFileName = args[0];
				// Output filename
				if (args.Length > 1)
				{
					outFileName = args[1];
				}
				else
				{
					outFileName = inFileName + ".txt";
				}
				convertFile(inFileName, outFileName);
			}
			Console.WriteLine("Finished.");
			Console.Write("Press any key to continue...");
			Console.ReadKey(true);
		}

		private static void convertFile(string inFileName, string outFileName)
		{
			try
			{
				Console.WriteLine("Converting " + Path.GetFileName(inFileName));
				// check Id
				numCh = getNumCh(getId(inFileName));

				// Read input file
				byte[] buff = File.ReadAllBytes(inFileName);
				// Read pattern name file (if exists)
				readMop(inFileName);

				// Create a file to write to
				using (StreamWriter sw = File.CreateText(outFileName))
				{
					// song title
					songTitle = "";
					for (int c = 0; c < 20; c++)
					{
						if (buff[c] != 0) songTitle += (char)buff[c];
					}

					// song length
					songLength = (int)buff[950];

					// print out data
					int sLen = 8 + songTitle.Length;
					printSeparator(sw, sLen);
					sw.WriteLine("Title: " + songTitle);
					sw.WriteLine("Channels: " + numCh);
					sw.WriteLine("Length: " + songLength);
					printSeparator(sw, sLen);
					sw.WriteLine();

					sw.WriteLine("Instruments:");
					printSeparator(sw, 12);
					// read instruments
					for (int i = 0; i < 31; i++)
					{
						int ip = 20 + i * 30;
						// instrument name
						string iName = "";
						for (int c = 0; c < 22; c++)
						{
							if (buff[ip + c] != 0) iName += (char)buff[ip + c]; else iName += " ";
						}
						// instrument length
						int iLength = ((buff[ip + 22] << 8) | buff[ip + 23]) * 2;
						// skip empty instuments
						if (iLength > 0)
						{
							sw.WriteLine(string.Format("[{0:X2}] [{1:X4}] [{2}]", i + 1, iLength, iName));
						}
					}
					sw.WriteLine();

					// pattern play sequence
					sw.WriteLine("Play sequence:");
					printSeparator(sw, 14);

					for (int p = 0; p < songLength; p++)
					{
						int next = buff[952 + p];
						// pattern names?
						if (mop.Count > 0)
						{
							printPatternInfo(sw, p, next);
						}
						else
						{
							sw.Write("[{0:D2}] ", next);
							if ((p + 1) % 8 == 0 || p == songLength - 1)
							{
								sw.WriteLine();
							}
						}
					}
					sw.WriteLine();

					// read patterns
					sLen = (mop.Count > 0 ? 43 : 24);
					for (int p = 0; p < songLength; p++)
					{
						int next = buff[952 + p];
						byte[] pattern = getPattern(buff, next);
						printSeparator(sw, sLen);
						printPatternInfo(sw, p, next);
						printSeparator(sw, sLen);
						// pattern notes
						for (int line = 0; line < 64; line++)
						{
							string text = "|";
							for (int ch = 0; ch < numCh; ch++)
							{
								int np = line * numCh * 4 + ch * 4;
								// b0: instrument number is in bits 12-15
								// b2: Upper nibble : Lower 4 bits of the instrument
								int instr = (pattern[np] & 0xf0) | ((pattern[np + 2] & 0xf0) >> 4);
								// w0: 12-bit period in bits 0-11
								int period = ((pattern[np] & 0x0f) << 8) | pattern[np + 1];
								// b2: Lower nibble : Special effect command
								int effCmd = pattern[np + 2] & 0x0f;
								// b3: Special effects data
								int effData = pattern[np + 3];
								// convert to text
								text += string.Format(
									" {0} {1:X2} {2:X1}{3:X2} |",
									period == 0 ? "---" : getNote(period),
									instr, effCmd, effData
								);
							}
							sw.WriteLine(text);
						}
						sw.WriteLine();
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private static void printSeparator(StreamWriter sw, int len)
		{
			for (int c = 0; c < len; c++)
			{
				sw.Write("=");
			}
			sw.WriteLine();
		}

		private static void printPatternInfo(StreamWriter sw, int pos, int num)
		{
			sw.Write(string.Format("[Pos: {0:D3}] [Pattern: {1:D2}]", pos, num));

			if (mop.Contains(num))
			{
				sw.Write(string.Format(" [{0}]", mop[num]));
			}
			sw.WriteLine();
		}

		private static string getId(string fileName)
		{
			string id = "";
			using (FileStream fs = File.OpenRead(fileName))
			{
				fs.Seek(1080, SeekOrigin.Begin);
				for (int c = 0; c < 4; c++)
				{
					int nextByte = fs.ReadByte();
					if (nextByte != -1) id += (char)nextByte;
				}
			}
			return id;
		}

		private static int getNumCh(string id)
		{
			switch (id)
			{
				case "M.K.": // standard 4-channel, 64-pattern-max MOD
				case "M!K!": // ProTracker will write this if there's more than 64 patterns
				case "M&K!": // This is just a standard MOD, but with a weird tag
				case "4CHN": // 4-channel MODs
					return 4;

				//* NOT SUPPORTING THESE (YET) *//
				case "6CHN": // 6-channel MODs, read like a 4 channel mod, but with 6 channels per row
				case "8CHN": // 8-channel MODs, read like a 4 channel mod, but with 8 channels per row

				case "CD81": // other 8-channel MOD tags, Oktalyzer / OctaMED
				case "OKTA":
				case "OCTA":

				case "2CHN": // a 2 channel MOD. This is handled by FastTracker

				case "TDZ1": // allegedly this is a TakeTracker extension
				case "TDZ2": //  for 1, 2, and 3 channels respectively
				case "TDZ3":

				case "5CHN": // allegedly this is a TakeTracker extension
				case "7CHN": //  for 5, 7, and 8 channels respectively
				case "9CHN":

				case "FLT4": // StarTrekker 4-channel MOD
				case "4FLT":

				case "FLT8": // StarTrekker 8-channel MOD
				case "8FLT":

				case "xxCH": // 10+ channel MOD, xx being a decimal number
				case "xxCN":
					throw new Exception("This format is not supported!");

				default:
					throw new Exception("Unknown Id or not a module file!");
			}
		}

		private static void readMop(string fileName)
		{
			string cDir = Path.GetDirectoryName(fileName);
			string modName = Path.GetFileNameWithoutExtension(fileName);
			string mopFile = Path.Combine(cDir, modName + ".mop");

			mop.Clear();
			// read pattern names file
			if (File.Exists(mopFile))
			{
				FileInfo fi = new FileInfo(mopFile);
				if (fi.Length != 1600) return;

				byte[] mopData = File.ReadAllBytes(mopFile);

				for (int p = 0; p < fi.Length / 16; p++)
				{
					// pattern name
					string pName = "";
					bool empty = true;
					for (int c = 0; c < 16; c++)
					{
						int pp = p * 16;
						byte next = mopData[pp + c];
						if (next != 0) pName += (char)next; else pName += " ";
						if (next != 0 && next != 32) empty = false;
					}
					if (!empty) mop.Add(p, pName);
				}
			}
		}

		private static byte[] getPattern(byte[] buff, int num)
		{
			int size = 64 * numCh * 4;
			byte[] pattern = new byte[size];

			for (int i = 0; i < size; i++) pattern[i] = buff[1084 + num * size + i];
			return pattern;
		}

		private static string getNote(int period)
		{
			int[] mod = {
				1712, 1616, 1524, 1440, 1356, 1280, 1208, 1140, 1076, 1016, 960, 906,
				856, 808, 762, 720, 678, 640, 604, 570, 538, 508, 480, 453,
				428, 404, 381, 360, 339, 320, 302, 285, 269, 254, 240, 226,
				214, 202, 190, 180, 170, 160, 151, 143, 135, 127, 120, 113,
				107, 101, 95, 90, 85, 80, 75, 71, 67, 63, 60, 56
			};

			string[] note = {
				"C-0", "C#0", "D-0", "D#0", "E-0", "F-0", "F#0", "G-0", "G#0", "A-0", "A#0", "B-0",
				"C-1", "C#1", "D-1", "D#1", "E-1", "F-1", "F#1", "G-1", "G#1", "A-1", "A#1", "B-1",
				"C-2", "C#2", "D-2", "D#2", "E-2", "F-2", "F#2", "G-2", "G#2", "A-2", "A#2", "B-2",
				"C-3", "C#3", "D-3", "D#3", "E-3", "F-3", "F#3", "G-3", "G#3", "A-3", "A#3", "B-3",
				"C-4", "C#4", "D-4", "D#4", "E-4", "F-4", "F#4", "G-4", "G#4", "A-4", "A#4", "B-4"
			};

			for (int i = 0; i < 12 * 5; i++)
			{
				if (mod[i] == period) return note[i];
			}
			return "???";
		}
	}
}