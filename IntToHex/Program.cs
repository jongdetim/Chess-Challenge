using System;

public class Program
{
	public static void Main()
	{
		int[] pawnTable = { 0, 0, 0, 0, 0, 0, 0, 0,
							5, 10, 10, -20, -20, 10, 10, 5,
							5, -5, -10, 0, 0, -10, -5, 5,
							0, 0, 0, 20, 20, 0, 0, 0,
							5, 5, 10, 25, 25, 10, 5, 5,
							10, 10, 20, 30, 30, 20, 10, 10,
							50, 50, 50, 50, 50, 50, 50, 50,
							0, 0, 0, 0, 0, 0, 0, 0 };
		int[] knightTable = {
		-50, -40, -30, -30, -30, -30, -40, -50,
		-40, -20, 0, 5, 5, 0, -20, -40,
		-30, 5, 10, 15, 15, 10, 5, -30,
		-30, 0, 15, 20, 20, 15, 0, -30,
		-30, 5, 15, 20, 20, 15, 5, -30,
		-30, 0, 10, 15, 15, 10, 0, -30,
		-40, -20, 0, 0, 0, 0, -20, -40,
		-50, -40, -30, -30, -30, -30, -40, -50};

		int[] bishopTable = {
			-20, -10, -10, -10, -10, -10, -10, -20,
			-10, 5, 0, 0, 0, 0, 5, -10,
			-10, 10, 10, 10, 10, 10, 10, -10,
			-10, 0, 10, 10, 10, 10, 0, -10,
			-10, 5, 5, 10, 10, 5, 5, -10,
			-10, 0, 5, 10, 10, 5, 0, -10,
			-10, 0, 0, 0, 0, 0, 0, -10,
			-20, -10, -10, -10, -10, -10, -10, -20};

		int[] rookTable = {
			0, 0, 0, 5, 5, 0, 0, 0,
			-5, 0, 0, 0, 0, 0, 0, -5,
			-5, 0, 0, 0, 0, 0, 0, -5,
			-5, 0, 0, 0, 0, 0, 0, -5,
			-5, 0, 0, 0, 0, 0, 0, -5,
			-5, 0, 0, 0, 0, 0, 0, -5,
			5, 10, 10, 10, 10, 10, 10, 5,
			0, 0, 0, 0, 0, 0, 0, 0};

		int[] queenTable = {
			-20, -10, -10, -5, -5, -10, -10, -20,
			-10, 0, 0, 0, 0, 0, 0, -10,
			-10, 5, 5, 5, 5, 5, 0, -10,
			0, 0, 5, 5, 5, 5, 0, -5,
			-5, 0, 5, 5, 5, 5, 0, -5,
			-10, 0, 5, 5, 5, 5, 0, -10,
			-10, 0, 0, 0, 0, 0, 0, -10,
			-20, -10, -10, -5, -5, -10, -10, -20};

		int[] kingTable = {
			20, 30, 10, 0, 0, 10, 30, 20,
			20, 20, 0, 0, 0, 0, 20, 20,
			-10, -20, -20, -20, -20, -20, -20, -10,
			-20, -30, -30, -40, -40, -30, -30, -20,
			-30, -40, -40, -50, -50, -40, -40, -30,
			-30, -40, -40, -50, -50, -40, -40, -30,
			-30, -40, -40, -50, -50, -40, -40, -30,
			-30, -40, -40, -50, -50, -40, -40, -30};

		int[][] piecePositionTable = { pawnTable, knightTable, bishopTable, rookTable, queenTable, kingTable };

		// convert each 8-bit int to 8-bit binary hex string, and then combine every 8 of them to 64-bit binary string in hex format
		foreach (int[] pieceTable in piecePositionTable)
		{
			string[] hexTable = new string[64];
			for (int i = 0; i < 64; i++)
			{
				hexTable[i] = Convert.ToString((byte)pieceTable[i], 16).ToUpper();
				if (hexTable[i].Length == 1)
				{
					hexTable[i] = "0" + hexTable[i];
				}
			}
			string[] hexStrings = new string[4];
			for (int i = 0; i < 8; i += 2)
			{
				hexStrings[i / 2] = string.Join("", hexTable.Skip(i * 8).Take(4));
				hexStrings[i / 2] = hexStrings[i / 2] + string.Join("", hexTable.Skip(i * 8 + 8).Take(4));
			}
			Console.Write("{");
			foreach (string str in hexStrings)
				Console.Write("0x" + str + ", ");
			Console.Write("}");
			Console.WriteLine();
		}

	}
}