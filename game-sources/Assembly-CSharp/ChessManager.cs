using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ULTRAKILL.Cheats;
using Unity.Mathematics;
using UnityEngine;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class ChessManager : MonoSingleton<ChessManager>
{
	public enum SpecialMove
	{
		None,
		ShortCastle,
		LongCastle,
		PawnTwoStep,
		PawnPromotion,
		EnPassantCapture
	}

	public struct MoveData
	{
		public int2 StartPosition;

		public ChessPieceData PieceToMove;

		public int2 EndPosition;

		public ChessPieceData CapturePiece;

		public SpecialMove SpecialMove;

		public int2 LastEnPassantPos;

		public ChessPieceData.PieceType PromotionType;

		public MoveData(ChessPieceData pieceToMove, int2 startPosition, ChessPieceData capturePiece, int2 endPosition, int2 lastEPPos, SpecialMove specialMove = SpecialMove.None, ChessPieceData.PieceType promotionType = ChessPieceData.PieceType.Pawn)
		{
			PieceToMove = pieceToMove;
			StartPosition = startPosition;
			EndPosition = endPosition;
			CapturePiece = capturePiece;
			SpecialMove = specialMove;
			LastEnPassantPos = lastEPPos;
			PromotionType = promotionType;
		}
	}

	public GameObject originalPieces;

	public GameObject originalExtras;

	public GameObject blackWinner;

	public GameObject whiteWinner;

	public GameObject blackOpponent;

	public GameObject whiteOpponent;

	public GameObject draw;

	public Transform helperTileGroup;

	private Renderer[] helperTiles = new Renderer[64];

	private MaterialPropertyBlock colorSetter;

	private Bounds colBounds;

	private GameObject clonedPieces;

	private ChessPieceData[] chessBoard = new ChessPieceData[64];

	private Dictionary<ChessPieceData, ChessPiece> allPieces = new Dictionary<ChessPieceData, ChessPiece>();

	private ChessPieceData whiteKing;

	private ChessPieceData blackKing;

	private int2 enPassantPos = new int2(-1, -1);

	private List<MoveData> legalMoves = new List<MoveData>(27);

	private List<MoveData> pseudoLegalMoves = new List<MoveData>(27);

	private List<MoveData> allLegalMoves = new List<MoveData>(27);

	private UciChessEngine chessEngine;

	private List<string> UCIMoves = new List<string>();

	[HideInInspector]
	public bool isWhiteTurn = true;

	[HideInInspector]
	public bool whiteIsBot;

	[HideInInspector]
	public bool blackIsBot = true;

	[HideInInspector]
	public bool gameLocked = true;

	private bool tutorialMessageSent;

	private int numMoves;

	public int elo = 1000;

	private static readonly int2[] pawnMoves = new int2[2]
	{
		new int2(0, 1),
		new int2(0, 2)
	};

	private static readonly int2[] pawnCaptures = new int2[2]
	{
		new int2(1, 1),
		new int2(-1, 1)
	};

	private static readonly int2[] rookDirections = new int2[4]
	{
		new int2(1, 0),
		new int2(-1, 0),
		new int2(0, 1),
		new int2(0, -1)
	};

	private static readonly int2[] bishopDirections = new int2[4]
	{
		new int2(1, 1),
		new int2(-1, 1),
		new int2(1, -1),
		new int2(-1, -1)
	};

	private static readonly int2[] queenDirections = rookDirections.Concat(bishopDirections).ToArray();

	private static readonly int2[] knightOffsets = new int2[8]
	{
		new int2(1, 2),
		new int2(2, 1),
		new int2(2, -1),
		new int2(1, -2),
		new int2(-1, -2),
		new int2(-2, -1),
		new int2(-2, 1),
		new int2(-1, 2)
	};

	private static readonly int2[] kingDirections = new int2[8]
	{
		new int2(1, 0),
		new int2(-1, 0),
		new int2(0, 1),
		new int2(0, -1),
		new int2(1, 1),
		new int2(-1, 1),
		new int2(1, -1),
		new int2(-1, -1)
	};

	private void Awake()
	{
		gameLocked = true;
		colBounds = GetComponent<Collider>().bounds;
		colorSetter = new MaterialPropertyBlock();
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				Renderer component = helperTileGroup.GetChild(i).GetChild(j).GetComponent<Renderer>();
				component.SetPropertyBlock(colorSetter);
				helperTiles[i + j * 8] = component;
			}
		}
	}

	private void Start()
	{
		ResetBoard();
	}

	public void SetupNewGame()
	{
		StopAllCoroutines();
		ResetBoard();
		gameLocked = false;
		if (!whiteIsBot || !blackIsBot)
		{
			MonoSingleton<CheatsManager>.Instance.GetCheatInstance<SummonSandboxArm>()?.TryCreateArmType(SpawnableType.MoveHand);
			if (!tutorialMessageSent)
			{
				MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("Chess pieces can be moved with the <color=orange>mover arm</color>.");
				tutorialMessageSent = true;
			}
		}
	}

	public void ToggleWhiteBot(bool isBot)
	{
		whiteIsBot = isBot;
		whiteOpponent.SetActive(whiteIsBot);
	}

	public void ToggleBlackBot(bool isBot)
	{
		blackIsBot = isBot;
		blackOpponent.SetActive(blackIsBot);
	}

	public void ResetBoard()
	{
		numMoves = 0;
		blackWinner.SetActive(value: false);
		whiteWinner.SetActive(value: false);
		HideHelperTiles();
		UCIMoves.Clear();
		if (clonedPieces != null)
		{
			UnityEngine.Object.Destroy(clonedPieces);
		}
		foreach (ChessPiece value in allPieces.Values)
		{
			if (value != null)
			{
				UnityEngine.Object.Destroy(value.gameObject);
			}
		}
		clonedPieces = null;
		clonedPieces = UnityEngine.Object.Instantiate(originalPieces, base.transform.parent);
		clonedPieces.SetActive(value: true);
		originalPieces.SetActive(value: false);
		allPieces.Clear();
		for (int i = 0; i < chessBoard.Length; i++)
		{
			chessBoard[i] = null;
		}
		isWhiteTurn = true;
		whiteOpponent.SetActive(whiteIsBot);
		blackOpponent.SetActive(blackIsBot);
		if (whiteIsBot || blackIsBot)
		{
			StartEngine();
		}
	}

	public void UpdateGame(MoveData move)
	{
		gameLocked = false;
		string text = ChessStringHandler.UCIMove(move);
		if (UCIMoves.Count > 0 && UCIMoves[UCIMoves.Count - 1].Equals(text))
		{
			Debug.LogError("tried to perform the same move twice");
			return;
		}
		UCIMoves.Add(text);
		if (UCIMoves.Count == 3 && UCIMoves[0] == "e2e4" && UCIMoves[1] == "e7e5" && UCIMoves[2] == "e1e2")
		{
			MonoSingleton<StyleHUD>.Instance.AddPoints(420, "<color=green>BONGCLOUD</color>");
		}
		string newMoveData = string.Join(" ", UCIMoves);
		if (isWhiteTurn)
		{
			numMoves++;
		}
		isWhiteTurn = !isWhiteTurn;
		if (IsGameOver())
		{
			if (numMoves == 2)
			{
				MonoSingleton<StyleHUD>.Instance.AddPoints(1, "<color=red>FOOLS MATE</color>");
			}
		}
		else if ((isWhiteTurn && whiteIsBot) || (!isWhiteTurn && blackIsBot))
		{
			StartCoroutine(SendToBotCoroutine(newMoveData));
		}
	}

	private bool IsGameOver()
	{
		if (!IsSufficientMaterial())
		{
			WinTrigger(null);
			return true;
		}
		allLegalMoves.Clear();
		for (int i = 0; i < chessBoard.Length; i++)
		{
			ChessPieceData chessPieceData = chessBoard[i];
			if (chessPieceData != null && chessPieceData.isWhite == isWhiteTurn)
			{
				GetLegalMoves(new int2(i % 8, i / 8));
				allLegalMoves.AddRange(legalMoves);
			}
		}
		if (allLegalMoves.Count == 0)
		{
			if (IsSquareAttacked(GetPiecePos(isWhiteTurn ? whiteKing : blackKing), isWhiteTurn))
			{
				WinTrigger(!isWhiteTurn);
			}
			else
			{
				WinTrigger(null);
			}
			return true;
		}
		return false;
	}

	public bool IsSufficientMaterial()
	{
		int num = 0;
		int num2 = 0;
		bool flag = false;
		bool flag2 = false;
		int2? @int = null;
		int2? int2 = null;
		for (int i = 0; i < chessBoard.Length; i++)
		{
			ChessPieceData chessPieceData = chessBoard[i];
			if (chessPieceData == null || chessPieceData.type == ChessPieceData.PieceType.King)
			{
				continue;
			}
			int2 value = new int2(i % 8, i / 8);
			if (chessPieceData.isWhite)
			{
				num++;
				if (chessPieceData.type == ChessPieceData.PieceType.Bishop)
				{
					flag = true;
					@int = value;
				}
			}
			else
			{
				num2++;
				if (chessPieceData.type == ChessPieceData.PieceType.Bishop)
				{
					flag2 = true;
					int2 = value;
				}
			}
			if (num > 1 || num2 > 1 || chessPieceData.type == ChessPieceData.PieceType.Pawn || chessPieceData.type == ChessPieceData.PieceType.Rook || chessPieceData.type == ChessPieceData.PieceType.Queen)
			{
				return true;
			}
		}
		if (flag && flag2)
		{
			return (@int.Value.x + @int.Value.y) % 2 != (int2.Value.x + int2.Value.y) % 2;
		}
		return false;
	}

	public void WinTrigger(bool? whiteWin)
	{
		gameLocked = true;
		StopEngine();
		if (!whiteWin.HasValue)
		{
			draw.GetComponent<AudioSource>().Play();
			return;
		}
		GameObject gameObject = (whiteWin.Value ? whiteWinner : blackWinner);
		gameObject.SetActive(value: true);
		AudioSource[] components = gameObject.GetComponents<AudioSource>();
		for (int i = 0; i < components.Length; i++)
		{
			components[i].Play();
		}
		gameObject.GetComponent<ParticleSystem>().Play();
		if ((whiteWin == true && !whiteIsBot) || (whiteWin == false && !blackIsBot))
		{
			MonoSingleton<StyleHUD>.Instance.AddPoints(5000, "<color=orange>" + ((whiteWin == true) ? "WHITE" : "BLACK") + " WINS</color>");
		}
		if ((whiteWin == true && !whiteIsBot && blackIsBot) || (whiteWin == false && !blackIsBot && whiteIsBot))
		{
			MonoSingleton<StyleHUD>.Instance.AddPoints(5000, "<color=red>ULTRAVICTORY</color>");
		}
	}

	public void SetElo(float newElo)
	{
		elo = Mathf.FloorToInt(newElo);
	}

	public void WhiteIsBot(bool isBot)
	{
		whiteIsBot = isBot;
	}

	public void BlackIsBot(bool isBot)
	{
		blackIsBot = isBot;
	}

	private async void StartEngine()
	{
		chessEngine = new UciChessEngine();
		await chessEngine.InitializeUciModeAsync(whiteIsBot, elo);
	}

	public async void StopEngine()
	{
		if (chessEngine != null)
		{
			await chessEngine.StopEngine();
			chessEngine = null;
		}
	}

	public void BotStartGame()
	{
		StartCoroutine(SendToBotCoroutine(""));
	}

	private IEnumerator SendToBotCoroutine(string newMoveData)
	{
		bool isResponseReceived = false;
		string response = "";
		if (elo < 1500)
		{
			int num = elo - 1000;
			chessEngine.SendPlayerMoveAndGetEngineResponseAsync(newMoveData, onReceivedResponse, 250 + num);
		}
		else
		{
			chessEngine.SendPlayerMoveAndGetEngineResponseAsync(newMoveData, onReceivedResponse);
		}
		yield return new WaitUntil(() => isResponseReceived);
		if (response.StartsWith("bestmove"))
		{
			string botMove = ParseBotMove(response);
			MakeBotMove(botMove);
		}
		void onReceivedResponse(string resp)
		{
			response = resp;
			isResponseReceived = true;
		}
	}

	private string ParseBotMove(string engineResponse)
	{
		string[] array = engineResponse.Split(' ');
		if (array.Length >= 2)
		{
			return array[1];
		}
		return string.Empty;
	}

	private IEnumerator LerpBotMove(ChessPiece piece, int2 endIndex, MoveData moveData)
	{
		Transform trans = piece.transform;
		Vector3 startPos = trans.position;
		Vector3 endPos = IndexToWorldPosition(endIndex, piece.boardHeight);
		float duration = UnityEngine.Random.Range(0.5f, 1f);
		float elapsed = 0f;
		if (UnityEngine.Random.Range(0, 1000) == 666)
		{
			duration = 15f;
		}
		piece.dragSound.SetPitch(UnityEngine.Random.Range(0.75f, 1.25f));
		piece.dragSound.Play();
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			trans.position = Vector3.Lerp(startPos, endPos, t);
			yield return null;
		}
		piece.dragSound.Stop();
		UnityEngine.Object.Instantiate(piece.snapSound, piece.transform.position, Quaternion.identity);
		yield return null;
		MakeMove(moveData, updateVisuals: true);
		yield return null;
	}

	private void MakeBotMove(string botMove)
	{
		(int2, int2, ChessPieceData.PieceType) tuple = ChessStringHandler.ProcessFullMove(botMove);
		int2 item = tuple.Item1;
		int2 endPos = tuple.Item2;
		ChessPieceData.PieceType promotionType = tuple.Item3;
		ChessPieceData pieceAt = GetPieceAt(item);
		if (pieceAt == null)
		{
			Debug.LogError("found no piece for move " + botMove);
		}
		GetLegalMoves(item);
		MoveData moveData = legalMoves.FirstOrDefault((MoveData move) => move.EndPosition.Equals(endPos) && move.PromotionType == promotionType);
		if (moveData.EndPosition.Equals(endPos))
		{
			ChessPiece piece = allPieces[pieceAt];
			StartCoroutine(LerpBotMove(piece, endPos, moveData));
		}
	}

	public int2 WorldPositionToIndex(Vector3 pos)
	{
		Vector3 min = colBounds.min;
		Vector3 max = colBounds.max;
		Vector3 vector = new Vector3((pos.x - min.x) / (max.x - min.x), 0f, (pos.z - min.z) / (max.z - min.z));
		int x = Mathf.FloorToInt(vector.x * 8f);
		int y = Mathf.FloorToInt(vector.z * 8f);
		return new int2(x, y);
	}

	public Vector3 IndexToWorldPosition(int2 index, float height)
	{
		Vector3 min = colBounds.min;
		Vector3 max = colBounds.max;
		float num = (float)Mathf.Clamp(index.x, 0, 7) + 0.5f;
		float num2 = (float)Mathf.Clamp(index.y, 0, 7) + 0.5f;
		return new Vector3(min.x + num * (max.x - min.x) / 8f, height, min.z + num2 * (max.z - min.z) / 8f);
	}

	public void DisplayValidMoves()
	{
		foreach (MoveData legalMove in legalMoves)
		{
			int x = legalMove.EndPosition.x;
			int y = legalMove.EndPosition.y;
			if (x >= 0 && x < 8 && y >= 0 && y < 8)
			{
				Renderer obj = helperTiles[x + y * 8];
				colorSetter.SetColor("_TintColor", (legalMove.CapturePiece != null) ? Color.green : Color.cyan);
				obj.SetPropertyBlock(colorSetter);
			}
			else
			{
				Debug.LogError("Trying to display a move out of range");
			}
		}
	}

	public void HideHelperTiles()
	{
		colorSetter.SetColor("_TintColor", Color.clear);
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				helperTiles[i + j * 8].SetPropertyBlock(colorSetter);
			}
		}
	}

	public void FindMoveAtWorldPosition(ChessPiece chessPiece)
	{
		Vector3 position = chessPiece.transform.position;
		int2 tileID = WorldPositionToIndex(position);
		ChessPieceData data = chessPiece.Data;
		if (legalMoves.Count == 0)
		{
			chessPiece.UpdatePosition(GetPiecePos(data));
		}
		else
		{
			MoveData moveData = legalMoves.FirstOrDefault((MoveData move) => move.EndPosition.Equals(tileID));
			if (!moveData.EndPosition.Equals(tileID) || moveData.StartPosition.Equals(moveData.EndPosition))
			{
				chessPiece.UpdatePosition(GetPiecePos(data));
			}
			else
			{
				MakeMove(moveData, updateVisuals: true);
			}
		}
		HideHelperTiles();
	}

	public void InitializePiece(ChessPiece piece)
	{
		ChessPieceData data = piece.Data;
		allPieces.Add(data, piece);
		Vector3 position = piece.transform.position;
		int2 @int = WorldPositionToIndex(position);
		if (data.type == ChessPieceData.PieceType.King)
		{
			if (piece.isWhite)
			{
				whiteKing = data;
			}
			else
			{
				blackKing = data;
			}
		}
		SetPieceAt(@int, data);
		piece.UpdatePosition(@int);
	}

	public ChessPieceData GetPieceAt(int2 index)
	{
		return chessBoard[index.x + index.y * 8];
	}

	public void SetPieceAt(int2 index, ChessPieceData piece)
	{
		chessBoard[index.x + index.y * 8] = piece;
	}

	private int2 GetPiecePos(ChessPieceData piece)
	{
		int num = Array.IndexOf(chessBoard, piece);
		return new int2(num % 8, num / 8);
	}

	public void MakeMove(MoveData moveData, bool updateVisuals = false)
	{
		ChessPieceData pieceToMove = moveData.PieceToMove;
		int2 endPosition = moveData.EndPosition;
		if (moveData.SpecialMove == SpecialMove.EnPassantCapture)
		{
			SetPieceAt(endPosition + new int2(0, (!pieceToMove.isWhite) ? 1 : (-1)), null);
		}
		if (moveData.SpecialMove == SpecialMove.PawnTwoStep)
		{
			enPassantPos = endPosition + new int2(0, (!pieceToMove.isWhite) ? 1 : (-1));
		}
		else
		{
			enPassantPos = new int2(-1, -1);
		}
		pieceToMove.timesMoved++;
		SetPieceAt(moveData.StartPosition, null);
		SetPieceAt(endPosition, pieceToMove);
		if (moveData.SpecialMove == SpecialMove.ShortCastle || moveData.SpecialMove == SpecialMove.LongCastle)
		{
			int x = ((moveData.SpecialMove == SpecialMove.ShortCastle) ? 7 : 0);
			int x2 = ((moveData.SpecialMove == SpecialMove.ShortCastle) ? 5 : 3);
			int2 index = new int2(x, (!pieceToMove.isWhite) ? 7 : 0);
			int2 @int = new int2(x2, (!pieceToMove.isWhite) ? 7 : 0);
			ChessPieceData pieceAt = GetPieceAt(index);
			pieceAt.timesMoved++;
			SetPieceAt(index, null);
			SetPieceAt(@int, pieceAt);
			if (updateVisuals && allPieces.TryGetValue(pieceAt, out var value))
			{
				value.UpdatePosition(@int);
			}
		}
		if (moveData.SpecialMove == SpecialMove.PawnPromotion)
		{
			pieceToMove.type = moveData.PromotionType;
			if (updateVisuals)
			{
				ChessPiece chessPiece = allPieces[pieceToMove];
				if (chessPiece.autoControl)
				{
					chessPiece.PromoteVisualPiece();
				}
				else
				{
					gameLocked = true;
					foreach (KeyValuePair<ChessPieceData, ChessPiece> allPiece in allPieces)
					{
						allPiece.Value.PieceCanMove(canMove: false);
					}
					chessPiece.ShowPromotionGUI(moveData);
				}
			}
		}
		if (updateVisuals && allPieces.TryGetValue(pieceToMove, out var value2))
		{
			value2.UpdatePosition(endPosition);
			if (moveData.SpecialMove == SpecialMove.LongCastle || moveData.SpecialMove == SpecialMove.ShortCastle)
			{
				UnityEngine.Object.Instantiate(value2.teleportEffect, value2.transform.position, Quaternion.identity);
			}
			if (moveData.SpecialMove == SpecialMove.PawnPromotion)
			{
				UnityEngine.Object.Instantiate(value2.promotionEffect, value2.transform.position, Quaternion.identity);
			}
			if (!pieceToMove.autoControl && moveData.SpecialMove != SpecialMove.PawnPromotion)
			{
				StylishMove(moveData);
			}
		}
		if (updateVisuals && moveData.CapturePiece != null && allPieces.TryGetValue(moveData.CapturePiece, out var value3))
		{
			value3.Captured();
		}
		if (updateVisuals && (moveData.SpecialMove != SpecialMove.PawnPromotion || moveData.PieceToMove.autoControl))
		{
			UpdateGame(moveData);
		}
	}

	public void StylishMove(MoveData move)
	{
		if (move.SpecialMove == SpecialMove.LongCastle || move.SpecialMove == SpecialMove.ShortCastle)
		{
			MonoSingleton<StyleHUD>.Instance.AddPoints(50, "<color=#00ffffff>CASTLED</color>");
		}
		if (move.SpecialMove == SpecialMove.PawnPromotion)
		{
			MonoSingleton<StyleHUD>.Instance.AddPoints(500, "<color=green>" + move.PromotionType.ToString().ToUpper() + " PROMOTION</color>");
		}
		int num = 0;
		string text = "<color=white>";
		if (move.CapturePiece != null)
		{
			switch (move.CapturePiece.type)
			{
			case ChessPieceData.PieceType.Knight:
				num = 100;
				text = "<color=green>";
				break;
			case ChessPieceData.PieceType.Bishop:
				num = 100;
				text = "<color=green>";
				break;
			case ChessPieceData.PieceType.Rook:
				num = 200;
				text = "<color=orange>";
				break;
			case ChessPieceData.PieceType.Queen:
				num = 400;
				text = "<color=red>";
				break;
			}
			if (move.SpecialMove == SpecialMove.EnPassantCapture)
			{
				MonoSingleton<StyleHUD>.Instance.AddPoints(100, "<color=#00ffffff>EN PASSANT</color>");
			}
			else
			{
				MonoSingleton<StyleHUD>.Instance.AddPoints(100 + num, text + move.CapturePiece.type.ToString().ToUpper() + " CAPTURE</color>");
			}
		}
	}

	public void UnmakeMove(MoveData moveData, bool updateVisuals = false)
	{
		enPassantPos = moveData.LastEnPassantPos;
		ChessPieceData pieceToMove = moveData.PieceToMove;
		SetPieceAt(moveData.StartPosition, moveData.PieceToMove);
		int2 endPosition = moveData.EndPosition;
		if (moveData.SpecialMove == SpecialMove.EnPassantCapture)
		{
			SetPieceAt(endPosition, null);
			endPosition += new int2(0, (!pieceToMove.isWhite) ? 1 : (-1));
		}
		SetPieceAt(endPosition, moveData.CapturePiece);
		pieceToMove.timesMoved--;
		if (moveData.SpecialMove == SpecialMove.ShortCastle || moveData.SpecialMove == SpecialMove.LongCastle)
		{
			int x = ((moveData.SpecialMove == SpecialMove.ShortCastle) ? 7 : 0);
			int x2 = ((moveData.SpecialMove == SpecialMove.ShortCastle) ? 5 : 3);
			int2 index = new int2(x, (!pieceToMove.isWhite) ? 7 : 0);
			int2 index2 = new int2(x2, (!pieceToMove.isWhite) ? 7 : 0);
			ChessPieceData pieceAt = GetPieceAt(index2);
			pieceAt.timesMoved--;
			SetPieceAt(index2, null);
			SetPieceAt(index, pieceAt);
		}
		if (moveData.SpecialMove == SpecialMove.PawnPromotion)
		{
			pieceToMove.type = ChessPieceData.PieceType.Pawn;
		}
		if (updateVisuals && allPieces.TryGetValue(pieceToMove, out var value))
		{
			value.UpdatePosition(moveData.StartPosition);
		}
		if (updateVisuals && moveData.CapturePiece != null && allPieces.TryGetValue(moveData.CapturePiece, out var value2))
		{
			value2.UpdatePosition(endPosition);
		}
	}

	private bool IsValidPosition(int2 index)
	{
		if (index.x >= 0 && index.x < 8 && index.y >= 0)
		{
			return index.y < 8;
		}
		return false;
	}

	public void GetLegalMoves(int2 index)
	{
		ChessPieceData pieceAt = GetPieceAt(index);
		if (pieceAt == null)
		{
			int2 @int = index;
			Debug.LogError("Found no piece at " + @int.ToString());
		}
		pseudoLegalMoves.Clear();
		legalMoves.Clear();
		switch (pieceAt.type)
		{
		case ChessPieceData.PieceType.Pawn:
			GetPawnMoves(pieceAt, index, pseudoLegalMoves);
			break;
		case ChessPieceData.PieceType.Knight:
		case ChessPieceData.PieceType.King:
			GetKnightKingMoves(pieceAt, index, pseudoLegalMoves);
			break;
		case ChessPieceData.PieceType.Rook:
		case ChessPieceData.PieceType.Bishop:
		case ChessPieceData.PieceType.Queen:
			GetSlidingMoves(pieceAt, index, pseudoLegalMoves);
			break;
		}
		int2 position = GetPiecePos(pieceAt.isWhite ? whiteKing : blackKing);
		foreach (MoveData pseudoLegalMove in pseudoLegalMoves)
		{
			MakeMove(pseudoLegalMove);
			if (pseudoLegalMove.PieceToMove.type == ChessPieceData.PieceType.King)
			{
				position = pseudoLegalMove.EndPosition;
			}
			if (!IsSquareAttacked(position, pieceAt.isWhite))
			{
				legalMoves.Add(pseudoLegalMove);
			}
			UnmakeMove(pseudoLegalMove);
		}
	}

	private void GetPawnMoves(ChessPieceData pawn, int2 startPos, List<MoveData> validMoves)
	{
		int num = (pawn.isWhite ? 1 : (-1));
		int2 @int = startPos + pawnMoves[0] * num;
		if (GetPieceAt(@int) == null)
		{
			if (@int.y == (pawn.isWhite ? 7 : 0))
			{
				validMoves.Add(new MoveData(pawn, startPos, null, @int, enPassantPos, SpecialMove.PawnPromotion, ChessPieceData.PieceType.Queen));
				validMoves.Add(new MoveData(pawn, startPos, null, @int, enPassantPos, SpecialMove.PawnPromotion, ChessPieceData.PieceType.Rook));
				validMoves.Add(new MoveData(pawn, startPos, null, @int, enPassantPos, SpecialMove.PawnPromotion, ChessPieceData.PieceType.Bishop));
				validMoves.Add(new MoveData(pawn, startPos, null, @int, enPassantPos, SpecialMove.PawnPromotion, ChessPieceData.PieceType.Knight));
			}
			else
			{
				validMoves.Add(new MoveData(pawn, startPos, null, @int, enPassantPos));
			}
			if (pawn.timesMoved == 0)
			{
				int2 int2 = startPos + pawnMoves[1] * num;
				if (GetPieceAt(int2) == null)
				{
					validMoves.Add(new MoveData(pawn, startPos, null, int2, enPassantPos, SpecialMove.PawnTwoStep));
				}
			}
		}
		int2[] array = pawnCaptures;
		foreach (int2 int3 in array)
		{
			int2 int4 = startPos + int3 * num;
			if (!IsValidPosition(int4))
			{
				continue;
			}
			ChessPieceData pieceAt = GetPieceAt(int4);
			if (pieceAt != null && pieceAt.isWhite != pawn.isWhite)
			{
				if (@int.y == (pawn.isWhite ? 7 : 0))
				{
					validMoves.Add(new MoveData(pawn, startPos, pieceAt, int4, enPassantPos, SpecialMove.PawnPromotion, ChessPieceData.PieceType.Queen));
					validMoves.Add(new MoveData(pawn, startPos, pieceAt, int4, enPassantPos, SpecialMove.PawnPromotion, ChessPieceData.PieceType.Rook));
					validMoves.Add(new MoveData(pawn, startPos, pieceAt, int4, enPassantPos, SpecialMove.PawnPromotion, ChessPieceData.PieceType.Bishop));
					validMoves.Add(new MoveData(pawn, startPos, pieceAt, int4, enPassantPos, SpecialMove.PawnPromotion, ChessPieceData.PieceType.Knight));
				}
				else
				{
					validMoves.Add(new MoveData(pawn, startPos, pieceAt, int4, enPassantPos));
				}
			}
			if (enPassantPos.Equals(int4))
			{
				int2 index = new int2(enPassantPos.x, enPassantPos.y - num);
				ChessPieceData pieceAt2 = GetPieceAt(index);
				if (pieceAt2 != null && pieceAt2.isWhite != pawn.isWhite)
				{
					validMoves.Add(new MoveData(pawn, startPos, pieceAt2, enPassantPos, enPassantPos, SpecialMove.EnPassantCapture));
				}
			}
		}
	}

	private void GetSlidingMoves(ChessPieceData slidingPiece, int2 startPos, List<MoveData> validMoves)
	{
		int2[] array;
		switch (slidingPiece.type)
		{
		case ChessPieceData.PieceType.Bishop:
			array = bishopDirections;
			break;
		case ChessPieceData.PieceType.Rook:
			array = rookDirections;
			break;
		case ChessPieceData.PieceType.Queen:
			array = queenDirections;
			break;
		default:
			Debug.LogError("Invalid piece type for sliding moves");
			array = new int2[1];
			break;
		}
		int2[] array2 = array;
		foreach (int2 @int in array2)
		{
			int2 int2 = startPos;
			while (true)
			{
				int2 += @int;
				if (!IsValidPosition(int2))
				{
					break;
				}
				ChessPieceData pieceAt = GetPieceAt(int2);
				if (pieceAt != null)
				{
					if (pieceAt.isWhite != slidingPiece.isWhite)
					{
						validMoves.Add(new MoveData(slidingPiece, startPos, pieceAt, int2, enPassantPos));
					}
					break;
				}
				validMoves.Add(new MoveData(slidingPiece, startPos, null, int2, enPassantPos));
			}
		}
	}

	private void GetKnightKingMoves(ChessPieceData piece, int2 startPos, List<MoveData> validMoves)
	{
		int2[] array = ((piece.type == ChessPieceData.PieceType.Knight) ? knightOffsets : kingDirections);
		foreach (int2 @int in array)
		{
			int2 int2 = startPos + @int;
			if (IsValidPosition(int2))
			{
				ChessPieceData pieceAt = GetPieceAt(int2);
				if (pieceAt == null || pieceAt.isWhite != piece.isWhite)
				{
					validMoves.Add(new MoveData(piece, startPos, pieceAt, int2, enPassantPos));
				}
			}
		}
		if (piece.type == ChessPieceData.PieceType.King)
		{
			TryCastle(piece, startPos, isKingSide: true, validMoves);
			TryCastle(piece, startPos, isKingSide: false, validMoves);
		}
	}

	private void TryCastle(ChessPieceData king, int2 startPos, bool isKingSide, List<MoveData> validMoves)
	{
		if (king.timesMoved > 0 || IsSquareAttacked(startPos, king.isWhite))
		{
			return;
		}
		int num = (isKingSide ? 7 : 0);
		int2 index = new int2(num, startPos.y);
		ChessPieceData pieceAt = GetPieceAt(index);
		if (pieceAt == null || pieceAt.isWhite != king.isWhite || pieceAt.type != ChessPieceData.PieceType.Rook || pieceAt.timesMoved > 0)
		{
			return;
		}
		int num2 = (isKingSide ? 1 : (-1));
		for (int i = startPos.x + num2; i != num; i += num2)
		{
			int2 index2 = new int2(i, startPos.y);
			if (GetPieceAt(index2) != null)
			{
				return;
			}
		}
		int2 position = new int2(startPos.x + num2, startPos.y);
		if (!IsSquareAttacked(position, king.isWhite))
		{
			SpecialMove specialMove = (isKingSide ? SpecialMove.ShortCastle : SpecialMove.LongCastle);
			validMoves.Add(new MoveData(king, startPos, null, new int2(isKingSide ? 6 : 2, startPos.y), enPassantPos, specialMove));
		}
	}

	public bool IsSquareAttacked(int2 position, bool isWhite)
	{
		int2[] array = kingDirections;
		foreach (int2 @int in array)
		{
			if (IsPieceAtPositionOfType(position + @int, isWhite, ChessPieceData.PieceType.King))
			{
				return true;
			}
		}
		if (IsSlidingPieceAttacking(position, isWhite, isRookMovement: true))
		{
			return true;
		}
		if (IsSlidingPieceAttacking(position, isWhite, isRookMovement: false))
		{
			return true;
		}
		array = knightOffsets;
		foreach (int2 int2 in array)
		{
			if (IsPieceAtPositionOfType(position + int2, isWhite, ChessPieceData.PieceType.Knight))
			{
				return true;
			}
		}
		int y = (isWhite ? 1 : (-1));
		array = pawnCaptures;
		foreach (int2 int3 in array)
		{
			if (IsPieceAtPositionOfType(position + int3 * new int2(1, y), isWhite, ChessPieceData.PieceType.Pawn))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsSlidingPieceAttacking(int2 position, bool isWhite, bool isRookMovement)
	{
		int2[] array = (isRookMovement ? rookDirections : bishopDirections);
		foreach (int2 @int in array)
		{
			for (int2 index = position + @int; IsValidPosition(index); index += @int)
			{
				ChessPieceData pieceAt = GetPieceAt(index);
				if (pieceAt != null)
				{
					if (pieceAt.isWhite == isWhite)
					{
						break;
					}
					if (pieceAt.type == ChessPieceData.PieceType.Queen)
					{
						return true;
					}
					if ((isRookMovement ? 1 : 3) != (int)pieceAt.type)
					{
						break;
					}
					return true;
				}
			}
		}
		return false;
	}

	private bool IsPieceAtPositionOfType(int2 position, bool isWhite, ChessPieceData.PieceType type)
	{
		if (IsValidPosition(position))
		{
			ChessPieceData pieceAt = GetPieceAt(position);
			if (pieceAt != null && pieceAt.isWhite != isWhite && pieceAt.type == type)
			{
				return true;
			}
		}
		return false;
	}
}
