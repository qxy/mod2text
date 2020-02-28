using System;
using System.IO;

namespace Mod2Text
{
	class Program
	{
		private static string inFileName, outFileName;

		private static string songTitle;
		private static int songLength, numCh;

		public static void Main(string[] args)
		{
			try
			{
				// Input filename
				if (args.Length < 1)
				{
					throw new Exception("Usage: Mod2Text \"input (mod) file name\" [\"output (text) file name\"]");
					//inFileName = "The_Mes_4.mod"; // debug
				}
				else
				{
					inFileName = args[0];
				}
				// Output filename
				if (args.Length > 1)
				{
					outFileName = args[1];
				}
				else
				{
					outFileName = inFileName + ".txt";
				}

				// Create a file to write to
	            using (StreamWriter sw = File.CreateText(outFileName))
	            {
					// Process input file
					byte[] buff = File.ReadAllBytes(inFileName);

					// song title
					char[] temp = new char[20];
					int i = 0; while (i < 20 && buff[i] != 0) temp[i] = (char)buff[i++];
					songTitle = new string(temp);
					// song length
					songLength = (int)buff[950];
					// Id
					temp = new char[4];
					for (i = 0; i < 4; i++) temp[i] = (char)buff[1080 + i];
					string id = new string(temp);
					numCh = getNumCh(id);
					// print out data
					sw.WriteLine("Title: " + songTitle);
					sw.WriteLine("Length: " + songLength + " patterns");
					sw.WriteLine("Channels: " + numCh);
					sw.WriteLine();
					// read patterns
					for (int p = 0; p < songLength; p++)
					{
						int next = buff[952 + p];
						byte[] pattern = getPattern(buff, next);
						sw.WriteLine(string.Format("[Pattern: {0:D3}]", next));

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
	            Console.WriteLine("Conversion done!");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
			finally
			{
				Console.Write("Press any key to continue . . . ");
				Console.ReadKey(true);
			}
		}

		private static int getNumCh(String id)
		{
			switch (id)
			{
				case "M.K.":
				case "M!K!":
				case "4CHN":
				case "4FLT":
					return 4;

				case "6CHN":
					return 6;

				case "8CHN":
				case "8FLT":
					return 8;

				default:
					throw new Exception("Unknown mod Id!");
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