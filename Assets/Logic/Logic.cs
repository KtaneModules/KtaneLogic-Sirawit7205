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

    public bool[] tog, truthTable, ans = { false, false };
    public int[] num, randomSym, paren = { 0, 0 };
    public int indcCounts;
    public string[] symText = { "AND", "OR" };

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
            checkAns();
            return false;
        };
    }

    void Init()
    {
        //Generate Letters
        for (int i = 0; i < 6; i++)
        {
            num[i] = Random.Range(0, 25) + 65;
            Text[i].text = char.ConvertFromUtf32(num[i]);
        }

        //Randomize parentheses and truth symbols
        paren[0] = Random.Range(0, 2);
        if(paren[0] == 0)
        {
            Text[0].color = Color.green;
            Text[1].color = Color.green;
        }
        else
        {
            Text[1].color = Color.green;
            Text[2].color = Color.green;
        }

        paren[1] = Random.Range(0, 2);
        if (paren[1] == 0)
        {
            Text[3].color = Color.green;
            Text[4].color = Color.green;
        }
        else
        {
            Text[4].color = Color.green;
            Text[5].color = Color.green;
        }

        for (int i = 0; i < 4; i++)
        {
            randomSym[i] = Random.Range(0, 2);
            if (randomSym[i] == 0) sym[i].text = "∧";
            else sym[i].text = "∨";
        }
        generateAns();
        _lightson = true;
    }

    void generateAns()
    {
        bool[] temp = { false, false, false, false, false, false };

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
        for (int i = 0; i < 6; i++) temp[i] = truthTable[num[i] - 65];
        Debug.LogFormat("[Logic #{0}] Truth table for symbols (row #1): {1} = {2} {3} = {4} {5} = {6}",
            _moduleId, (char)num[0], temp[0], (char)num[1], temp[1], (char)num[2], temp[2]);
		Debug.LogFormat("[Logic #{0}] Truth table for symbols (row #2): {1} = {2} {3} = {4} {5} = {6}",
            _moduleId, (char)num[3], temp[3], (char)num[4], temp[4], (char)num[5], temp[5]);

        for(int i=0;i<2;i++)
        {
            if (paren[i] == 0)
            {
                if (randomSym[i * 2] == 0 && randomSym[(i * 2) + 1] == 0)
                    if ((temp[i * 3] && temp[(i * 3) + 1]) && temp[(i * 3) + 2] == true) ans[i] = true;
                if (randomSym[i * 2] == 0 && randomSym[(i * 2) + 1] == 1)
                    if ((temp[i * 3] && temp[(i * 3) + 1]) || temp[(i * 3) + 2] == true) ans[i] = true;
                if (randomSym[i * 2] == 1 && randomSym[(i * 2) + 1] == 0)
                    if ((temp[i * 3] || temp[(i * 3) + 1]) && temp[(i * 3) + 2] == true) ans[i] = true;
                if (randomSym[i * 2] == 1 && randomSym[(i * 2) + 1] == 1)
                    if ((temp[i * 3] || temp[(i * 3) + 1]) || temp[(i * 3) + 2] == true) ans[i] = true;

                Debug.LogFormat("[Logic #{0}] Row #{1}: ({2} {3} {4}) {5} {6} = {7}", _moduleId, i + 1, (char)num[i * 3], symText[randomSym[i * 2]], (char)num[(i * 3) + 1], symText[randomSym[(i * 2) + 1]], (char)num[(i * 3) + 2], ans[i]);
            }
            else
            {
                if (randomSym[i * 2] == 0 && randomSym[(i * 2) + 1] == 0)
                    if (temp[i * 3] && (temp[(i * 3) + 1] && temp[(i * 3) + 2]) == true) ans[i] = true;
                if (randomSym[i * 2] == 0 && randomSym[(i * 2) + 1] == 1)
                    if (temp[i * 3] && (temp[(i * 3) + 1] || temp[(i * 3) + 2]) == true) ans[i] = true;
                if (randomSym[i * 2] == 1 && randomSym[(i * 2) + 1] == 0)
                    if (temp[i * 3] || (temp[(i * 3) + 1] && temp[(i * 3) + 2]) == true) ans[i] = true;
                if (randomSym[i * 2] == 1 && randomSym[(i * 2) + 1] == 1)
                    if (temp[i * 3] || (temp[(i * 3) + 1] || temp[(i * 3) + 2]) == true) ans[i] = true;

                Debug.LogFormat("[Logic #{0}] Row #{1}: {2} {3} ({4} {5} {6}) = {7}", _moduleId, i + 1, (char)num[i * 3], symText[randomSym[i * 2]], (char)num[(i * 3) + 1], symText[randomSym[(i * 2) + 1]], (char)num[(i * 3) + 2], ans[i]);
            }
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

    void checkAns()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttons[2].transform);
        buttons[2].AddInteractionPunch();
        if (!_isSolved && _lightson)
        {
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
