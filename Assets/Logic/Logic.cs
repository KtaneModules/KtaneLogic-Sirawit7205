using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using KMHelper;

public class Logic : MonoBehaviour
{
    public KMAudio Audio;
    public KMSelectable[] buttons;
    public TextMesh[] Text, sym;
    public KMBombInfo Bomb;
    public MeshRenderer[] notIndc;
    public GameObject[] parenObject;

    private readonly bool[] tog = { false, false }, ans = { false, false }, isNot = new bool[6], truthTable = new bool[26];
    private readonly int[] paren = { 0, 0 }, num = new int[6], randomSym = new int[4];
    private readonly int indcCounts;
    private readonly string[] symText = { "AND", "OR", "XOR", "NAND", "NOR", "XNOR", "→", "←" };

    private bool _isSolved = false, _lightson = false;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += Init;
    }

    void Awake()
    {
        buttons[0].OnInteract += delegate ()
        {
            HandlePress(0);
            return false;
        };
        buttons[1].OnInteract += delegate ()
        {
            HandlePress(1);
            return false;
        };
        buttons[2].OnInteract += delegate ()
        {
            CheckAns();
            return false;
        };
    }

    void Init()
    {
        //Randomize buttons
        RandomButtons(0);
        RandomButtons(1);

        //Generate Letters
        for (int i = 0; i < 6; i++)
        {
            num[i] = Random.Range(0, 25) + 65;
            Text[i].text = char.ConvertFromUtf32(num[i]);
        }

        //Randomize parentheses
        paren[0] = Random.Range(0, 2);
        if(paren[0] == 0)
        {
            parenObject[0].transform.localPosition = new Vector3(-0.076f, parenObject[0].transform.localPosition.y, parenObject[0].transform.localPosition.z);
            parenObject[1].transform.localPosition = new Vector3(0.002f, parenObject[1].transform.localPosition.y, parenObject[1].transform.localPosition.z);
        }
        else
        {
            parenObject[0].transform.localPosition = new Vector3(-0.028f, parenObject[0].transform.localPosition.y, parenObject[0].transform.localPosition.z);
            parenObject[1].transform.localPosition = new Vector3(0.051f, parenObject[1].transform.localPosition.y, parenObject[1].transform.localPosition.z);
        }

        paren[1] = Random.Range(0, 2);
        if (paren[1] == 0)
        {
            parenObject[2].transform.localPosition = new Vector3(-0.076f, parenObject[2].transform.localPosition.y, parenObject[2].transform.localPosition.z);
            parenObject[3].transform.localPosition = new Vector3(0.002f, parenObject[3].transform.localPosition.y, parenObject[3].transform.localPosition.z);
        }
        else
        {
            parenObject[2].transform.localPosition = new Vector3(-0.028f, parenObject[2].transform.localPosition.y, parenObject[2].transform.localPosition.z);
            parenObject[3].transform.localPosition = new Vector3(0.051f, parenObject[3].transform.localPosition.y, parenObject[3].transform.localPosition.z);
        }

        //logic symbols
        for (int i = 0; i < 4; i++)
        {
            randomSym[i] = Random.Range(0, 8);
            if (randomSym[i] == 0) sym[i].text = "∧"; //AND
            else if (randomSym[i] == 1) sym[i].text = "∨"; //OR
            else if (randomSym[i] == 2) sym[i].text = "⊻"; //XOR
            else if (randomSym[i] == 3) sym[i].text = "|"; //NAND
            else if (randomSym[i] == 4) sym[i].text = "↓"; //NOR
            else if (randomSym[i] == 5) sym[i].text = "↔"; //XNOR
            else if (randomSym[i] == 6) sym[i].text = "→"; //IMP LEFT
            else sym[i].text = "←"; //IMP RIGHT

            //adjust symbols sizes
            if (randomSym[i] >= 5 && randomSym[i] <= 7)
                sym[i].fontSize = 25;
            else if (randomSym[i] == 2)
                sym[i].fontSize = 40;
        }

        //NOT case
        for (int i = 0; i < 6; i++)
        {
            int rand = Random.Range(0, 2);
            if (rand == 1)
            {
                notIndc[i].material.color = Color.red;
                isNot[i] = true;
            } 
        }

        GenerateAns();
        _lightson = true;
    }

    void GenerateAns()
    {
        bool[] boolVal = { false, false, false, false, false, false };
        string[] dbgSym = { "", "", "", "", "", "" };

        //Generate truth table for all letters
        if (Bomb.GetBatteryCount() == Bomb.GetIndicators().Count()) truthTable[0] = true;
        if (Bomb.GetSerialNumberLetters().Count() > Bomb.GetSerialNumberNumbers().Count()) truthTable[1] = true;
        if (Bomb.IsIndicatorPresent(KMBombInfoExtensions.KnownIndicatorLabel.IND)) truthTable[2] = true;
        if (Bomb.IsIndicatorPresent(KMBombInfoExtensions.KnownIndicatorLabel.FRK)) truthTable[3] = true;
        if (Bomb.GetOffIndicators().Count() == 1) truthTable[4] = true;
        if (Bomb.GetPorts().Distinct().Count() > 1) truthTable[5] = true;
        if (Bomb.GetBatteryCount() >= 2) truthTable[6] = true;
        if (Bomb.GetBatteryCount() < 2) truthTable[7] = true;
        if (Bomb.GetSerialNumberNumbers().Last() % 2 == 1) truthTable[8] = true;
        if (Bomb.GetBatteryCount() > 4) truthTable[9] = true;
        if (Bomb.GetOnIndicators().Count() == 1) truthTable[10] = true;
        if (Bomb.GetIndicators().Count() > 2) truthTable[11] = true;
        if (Bomb.GetPorts().Distinct().Count() == Bomb.GetPorts().Count()) truthTable[12] = true;
        if (Bomb.GetBatteryHolderCount() > 2) truthTable[13] = true;
        if (Bomb.GetOnIndicators().Count() > 0 && Bomb.GetOffIndicators().Count() > 0) truthTable[14] = true;
        if (Bomb.IsPortPresent(KMBombInfoExtensions.KnownPortType.Parallel)) truthTable[15] = true;
        if (Bomb.GetPorts().Count() == 2) truthTable[16] = true;
        if (Bomb.IsPortPresent(KMBombInfoExtensions.KnownPortType.PS2)) truthTable[17] = true;
        if (Bomb.GetSerialNumberNumbers().Sum() > 10) truthTable[18] = true;
        if (Bomb.IsIndicatorPresent(KMBombInfoExtensions.KnownIndicatorLabel.MSA)) truthTable[19] = true;
        if (Bomb.GetBatteryHolderCount() == 1) truthTable[20] = true;
        if (Bomb.GetSerialNumberLetters().Any("AEIOU".Contains)) truthTable[21] = true;
        if (Bomb.GetIndicators().Count() == 0) truthTable[22] = true;
        if (Bomb.GetIndicators().Count() == 1) truthTable[23] = true;
        if (Bomb.GetPorts().Count() > 5) truthTable[24] = true;
        if (Bomb.GetPorts().Count() < 2) truthTable[25] = true;

        //Check Answers for each row
        for (int i = 0; i < 6; i++) boolVal[i] = truthTable[num[i] - 65];

        Debug.LogFormat("[Logic #{0}] Truth table for symbols (row #1): {1} = {2} {3} = {4} {5} = {6}",
            _moduleId, (char)num[0], boolVal[0], (char)num[1], boolVal[1], (char)num[2], boolVal[2]);
		Debug.LogFormat("[Logic #{0}] Truth table for symbols (row #2): {1} = {2} {3} = {4} {5} = {6}",
            _moduleId, (char)num[3], boolVal[3], (char)num[4], boolVal[4], (char)num[5], boolVal[5]);

        for (int i = 0; i < 6; i++)
        {
            if (isNot[i] == true)
            {
                boolVal[i] = !boolVal[i];
                dbgSym[i] = "¬" + (char)num[i];
            }
            else dbgSym[i] = "" + (char)num[i];
        }

        for (int i = 0; i < 2; i++)
        {
            bool retTemp;
            if(paren[i] == 0)
            {
                retTemp = CalcLogic(boolVal[i * 3], boolVal[(i * 3) + 1], randomSym[i * 2]);
                ans[i] = CalcLogic(retTemp, boolVal[(i * 3) + 2], randomSym[(i * 2) + 1]);

                Debug.LogFormat("[Logic #{0}] Row #{1}: ( {2} {3} {4} ) {5} {6} = {7}", _moduleId, i + 1,
                    dbgSym[i * 3], symText[randomSym[i * 2]], dbgSym[(i * 3) + 1], symText[randomSym[(i * 2) + 1]], dbgSym[(i * 3) + 2], ans[i]);
            }
            else
            {
                retTemp = CalcLogic(boolVal[(i * 3) + 1], boolVal[(i * 3) + 2], randomSym[(i * 2) + 1]);
                ans[i] = CalcLogic(boolVal[i * 3], retTemp, randomSym[i * 2]);

                Debug.LogFormat("[Logic #{0}] Row #{1}: {2} {3} ( {4} {5} {6} ) = {7}", _moduleId, i + 1,
                    dbgSym[i * 3], symText[randomSym[i * 2]], dbgSym[(i * 3) + 1], symText[randomSym[(i * 2) + 1]], dbgSym[(i * 3) + 2], ans[i]);
            }

        }
    }

    bool CalcLogic(bool left, bool right, int sym)
    {
        if(sym == 0) //AND
        {
            if (left == true && right == true) return true;
            else return false;
        }
        else if(sym == 1) //OR
        {
            if (left == true || right == true) return true;
            else return false;
        }
        else if(sym == 2) //XOR
        {
            if (left == true && right == false) return true;
            else if (left == false && right == true) return true;
            else return false;
        }
        else if (sym == 3) //NAND
        {
            if (left == true && right == true) return false;
            else return true;
        }
        else if (sym == 4) //NOR
        {
            if (left == true || right == true) return false;
            else return true;
        }
        else if (sym == 5) //XNOR
        {
            if (left == true && right == false) return false;
            else if (left == false && right == true) return false;
            else return true;
        }
        else if (sym == 6) //IMP LEFT
        {
            if (left == true && right == false) return false;
            else return true;
        }
        else //IMP RIGHT
        {
            if (left == false && right == true) return false;
            else return true;
        }
    }

    void HandlePress(int mode)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttons[mode].transform);
        buttons[mode].AddInteractionPunch();
        if (!_isSolved && _lightson)
        {
            if (tog[mode] == false)
            {
                buttons[mode].GetComponent<MeshRenderer>().material.color = Color.green;
                Text[mode + 6].text = "T";
                tog[mode] = true;
            }
            else
            {
                buttons[mode].GetComponent<MeshRenderer>().material.color = Color.red;
                Text[mode + 6].text = "F";
                tog[mode] = false;
            }
        }
    }

    void RandomButtons(int n)
    {
        int rand = Random.Range(0, 2);
        if(rand == 1)
        {
            buttons[n].GetComponent<MeshRenderer>().material.color = Color.green;
            Text[n + 6].text = "T";
            tog[n] = true;
        }
    }

    void CheckAns()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttons[2].transform);
        buttons[2].AddInteractionPunch();
        if (!_isSolved && _lightson)
        {
            Debug.LogFormat("[Logic #{0}] Submits {1} and {2}. Expects {3} and {4}.", _moduleId, tog[0], tog[1], ans[0], ans[1]);
            if (tog[0] == ans[0] && tog[1] == ans[1])
            {
                Debug.LogFormat("[Logic #{0}] Module solved.", _moduleId);
                GetComponent<KMBombModule>().HandlePass();
                _isSolved = true;
            }
            else
            {
                Debug.LogFormat("[Logic #{0}] 1st row: {1} (should have been {2}). 2nd row: {3} (should have been {4}).", _moduleId, tog[0], ans[0], tog[1], ans[1]);
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
    }

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        var btn = new List<KMSelectable>();
        int temp = 0;

        command = command.ToLowerInvariant().Trim();

        if (Regex.IsMatch(command, @"^submit \b(true|false|t|f)\b \b(true|false|t|f)\b$"))
        {
            command = command.Substring(7);
            foreach (var cell in command.Trim().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                if ((cell.Equals("t") || cell.Equals("true")) && tog[temp] == false) btn.Add(buttons[temp]);
                if ((cell.Equals("f") || cell.Equals("false")) && tog[temp] == true) btn.Add(buttons[temp]);
                temp++;
            }
            btn.Add(buttons[2]);
            return btn.ToArray();
        }

        else return null;
    }
}
