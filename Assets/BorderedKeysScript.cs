using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class BorderedKeysScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo bomb;
    public KMColorblindMode ColorblindMode;

    public List<KMSelectable> keys;
    public Renderer[] keyID;
    public Renderer[] keyHLID;
    public Material[] keyColours;
    public Material[] keyHLColours;

    private readonly int[][] table = new int[12][]{
       new int[18] { 4, 3, 5, 1, 6, 2, 3, 5, 2, 1, 6, 4, 6, 1, 3, 2, 5, 4},
       new int[18] { 2, 1, 6, 3, 4, 5, 1, 3, 6, 5, 4, 2, 4, 6, 1, 5, 3, 2},
       new int[18] { 5, 2, 1, 4, 3, 6, 6, 4, 1, 3, 2, 5, 1, 5, 2, 4, 6, 3},
       new int[18] { 3, 6, 2, 5, 1, 4, 2, 1, 5, 4, 3, 6, 3, 2, 6, 1, 4, 5},
       new int[18] { 1, 5, 4, 6, 2, 3, 4, 6, 3, 2, 5, 1, 5, 3, 4, 6, 2, 1},
       new int[18] { 6, 4, 3, 2, 5, 1, 5, 2, 4, 6, 1, 3, 2, 4, 5, 3, 1, 6},
       new int[18] { 1, 6, 3, 2, 4, 5, 3, 2, 6, 1, 5, 4, 4, 3, 6, 5, 2, 1},
       new int[18] { 3, 4, 2, 5, 1, 6, 6, 1, 5, 4, 3, 2, 3, 1, 5, 2, 4, 6},
       new int[18] { 6, 2, 1, 3, 5, 4, 1, 4, 2, 3, 6, 5, 5, 6, 2, 1, 3, 4},
       new int[18] { 5, 1, 6, 4, 2, 3, 2, 5, 1, 6, 4, 3, 1, 2, 3, 4, 6, 5},
       new int[18] { 4, 3, 5, 1, 6, 2, 4, 6, 3, 5, 2, 1, 2, 4, 1, 6, 5, 3},
       new int[18] { 2, 5, 4, 6, 3, 1, 5, 3, 4, 2, 1, 6, 6, 5, 4, 3, 1, 2}};
  
    private readonly string[] colourList = new string[6] { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow" };
    private int[][] info = new int[6][] { new int[5], new int[5], new int[5], new int[5], new int[5], new int[5] };
    private int resetCount;
    private int pressCount;
    private int currentCount;
    private int guarantee;
    private IEnumerator sequence;
    private bool starting = true;
    private bool pressable;
    private bool[] alreadypressed = new bool[7];
    private List<string> answer = new List<string> { };
    private bool colorblind;
    private string[] disp = new string[6];

    //Logging
    static int moduleCounter = 1;
    int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = moduleCounter++;
        sequence = Shuff();
        foreach (KMSelectable key in keys)
        {
            if (keys.IndexOf(key) != 6)
            {
                key.transform.localPosition = new Vector3(0, 0, -1f);
            }
            keyHLID[keys.IndexOf(key)].enabled = false;
            key.OnInteract += delegate () { KeyPress(key); return false; };
            key.OnHighlight += delegate () { KeyHL(keys.IndexOf(key)); };
            key.OnHighlightEnded += delegate () { KeyHLEnd(keys.IndexOf(key)); };
        }
    }

    void Start()
    {
        colorblind = ColorblindMode.ColorblindModeActive;
        Reset();
    }

    private void KeyHL(int k)
    {
        keyHLID[k].enabled = true;
        if (pressable == true && moduleSolved == false && k != 6 && alreadypressed[k] == false)
        {
            keys[6].transform.GetChild(2).GetComponent<TextMesh>().text = disp[k];
        }
    }

    private void KeyHLEnd(int k)
    {
        keyHLID[k].enabled = false;
        if (pressable == true && moduleSolved == false && k != 6)
        {
            keys[6].transform.GetChild(2).GetComponent<TextMesh>().text = string.Empty;
        }
    }

    private void KeyPress(KMSelectable key)
    {
        if (keys.IndexOf(key) == 6 && moduleSolved == false && pressable == true)
        {
            key.AddInteractionPunch();
            for (int i = 0; i < 7; i++)
            {
                if (i != 6)
                {
                    if (answer[i] == (pressCount - currentCount + 1).ToString() && alreadypressed[i] == false)
                    {
                        GetComponent<KMBombModule>().HandleStrike();
                        Debug.LogFormat("[Bordered Keys #{0}] Invalid reset", moduleID);
                        break;
                    }
                }
                else
                {
                    if (pressCount < 5)
                    {
                        resetCount++;
                        currentCount = 0;
                        answer.Clear();
                    }
                    else
                    {
                        moduleSolved = true;
                    }
                    Reset();
                }
            }
        }
        else if (alreadypressed[keys.IndexOf(key)] == false && moduleSolved == false && pressable == true)
        {
            key.AddInteractionPunch();
            if (answer[keys.IndexOf(key)] == (pressCount - currentCount + 1).ToString())
            {
                key.transform.localPosition = new Vector3(0, 0, -1f);
                GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
                alreadypressed[keys.IndexOf(key)] = true;
                if (pressCount < 5)
                {
                    pressCount++;
                    currentCount++;
                }
                else
                {
                    moduleSolved = true;
                    keys[6].transform.GetChild(2).GetComponent<TextMesh>().text = string.Empty;
                    StartCoroutine(sequence);
                }
            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Bordered Keys #{0}] Invalid key pressed: {1}", moduleID, keys.IndexOf(key) + 1);
            }
        }
    }

    private void setKey(int keyIndex)
    {
        keyID[keyIndex].material = keyColours[info[keyIndex][0]];
        keyHLID[keyIndex].material = keyColours[info[keyIndex][2]];
        switch (info[keyIndex][1])
        {
            case 0:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 25, 25, 255);
                break;
            case 1:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 255, 25, 255);
                break;
            case 2:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 25, 255, 255);
                break;
            case 3:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 255, 255, 255);
                break;
            case 4:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 75, 255, 255);
                break;
            case 5:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 255, 75, 255);
                break;
        }
        var label = (info[keyIndex][3] + 1).ToString();
        disp[keyIndex] = (info[keyIndex][4]).ToString();
        if (colorblind)
        {
            label += "\n" + "RGBCMY"[info[keyIndex][1]] + "\n\n" + "RGBCMY"[info[keyIndex][0]];
            disp[keyIndex] += " " + "RGBCMY"[info[keyIndex][2]];
        }
        keys[keyIndex].GetComponentInChildren<TextMesh>().text = label;
    }

    private void setRandomKey(int keyIndex, int rand1, int rand2, int rand3, int rand4, int rand5)
    {
        keyID[keyIndex].material = keyColours[rand1];
        keyHLID[keyIndex].material = keyHLColours[rand3];
        switch (rand2)
        {
            case 0:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 25, 25, 255);
                break;
            case 1:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 255, 25, 255);
                break;
            case 2:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 25, 255, 255);
                break;
            case 3:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 255, 255, 255);
                break;
            case 4:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 75, 255, 255);
                break;
            case 5:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 255, 75, 255);
                break;
        }
        var label = (rand4 + 1).ToString();
        if (colorblind)
        {
            label += "\n" + "RGBCMY"[info[keyIndex][1]] + "\n\n" + "RGBCMY"[info[keyIndex][0]];
        }
        keys[keyIndex].GetComponentInChildren<TextMesh>().text = label;
    }

    private void Reset()
    {
        if (moduleSolved == false)
        {
            guarantee = -1;
            while(guarantee == -1)
            {
                guarantee = Random.Range(0, 6);
                if(alreadypressed[guarantee] == true)
                {
                    guarantee = -1;
                }
            }
            for (int i = 0; i < 6; i++)
            {
                int valsum = 0;
                if (alreadypressed[i] == false)
                {
                    if (i == guarantee)
                    {
                        answer.Add(string.Empty);
                    }
                    else
                    {
                        int rand = Random.Range(pressCount + 1, 7);
                        answer.Add(rand.ToString());
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        info[i][j] = Random.Range(0, 6);
                    }
                    valsum = table[info[i][3]][info[i][0]] + table[info[i][3]][info[i][1] + 6] + table[info[i][3]][info[i][2] + 12] + table[i + 6][info[i][0]] + table[i + 6][info[i][1] + 6] + table[i + 6][info[i][2] + 12];
                    if (i == guarantee)
                    {
                        answer[guarantee] = (pressCount + 1).ToString();
                        while ((valsum + info[guarantee][4]) % 6 != pressCount)
                        {
                            info[guarantee][4]++;
                        }
                        info[guarantee][4] = (info[guarantee][4] % 6) + 1;
                    }
                    else
                    {
                        while ((((valsum + info[i][4]) % 6) + 1).ToString() != answer[i])
                        {
                            info[i][4]++;
                        }
                        info[i][4] = (info[i][4] % 6) + 1;
                    }
                }
                else
                {
                    answer.Add("/");
                }
            }
            string[] a = new string[6];
            string[] b = new string[6];
            string[] c = new string[6];
            string[] d = new string[6];
            string[] e = new string[6];
            for (int i = 0; i < 6; i++)
            {
                a[i] = colourList[info[i][0]];
                b[i] = colourList[info[i][1]];
                c[i] = colourList[info[i][2]];
                d[i] = (info[i][3] + 1).ToString();
                e[i] = (info[i][4]).ToString();
                if (i == 5)
                {
                    string A = string.Join(", ", a);
                    string B = string.Join(", ", b);
                    string C = string.Join(", ", c);
                    string D = string.Join(string.Empty, d);
                    string E = string.Join(string.Empty, e);
                    Debug.LogFormat("[Bordered Keys #{0}] After {1} reset(s), the keys had the colours: {2}", moduleID, resetCount, A);
                    Debug.LogFormat("[Bordered Keys #{0}] After {1} reset(s), the labels had the colours: {2}", moduleID, resetCount, B);
                    Debug.LogFormat("[Bordered Keys #{0}] After {1} reset(s), the borders had the colours: {2}", moduleID, resetCount, C);
                    Debug.LogFormat("[Bordered Keys #{0}] After {1} reset(s), the labels were: {2}", moduleID, resetCount, D);
                    Debug.LogFormat("[Bordered Keys #{0}] After {1} reset(s), the displays were: {2}", moduleID, resetCount, E);
                }
            }
            string[] answ = answer.ToArray();
            string ans = string.Join("", answ);
            Debug.LogFormat("[Bordered Keys #{0}] After {1} reset(s), the keys have the values: {2}", moduleID, resetCount, ans);
            List<string> f = new List<string> { };
            for (int i = 0; i < 6; i++)
            {
                if (answer[i] == (resetCount + 1).ToString())
                {
                    f.Add((i + 1).ToString());
                }
            }
            Debug.LogFormat("[Bordered Keys #{0}] Valid key(s) after {1} reset(s): {2}", moduleID, resetCount, string.Join(", ", f.ToArray()), resetCount);           
        }
        pressable = false;
        StartCoroutine(sequence);
    }

    private IEnumerator Shuff()
    {
        for (int i = 0; i < 30; i++)
        {
            if (moduleSolved == false && starting == false)
            {
                switch (i % 4)
                {
                    case 0:
                        keys[6].GetComponentInChildren<TextMesh>().text = "RESETTING";
                        break;
                    case 1:
                        keys[6].GetComponentInChildren<TextMesh>().text = "RESETTING.";
                        break;
                    case 2:
                        keys[6].GetComponentInChildren<TextMesh>().text = "RESETTING..";
                        break;
                    case 3:
                        keys[6].GetComponentInChildren<TextMesh>().text = "RESETTING...";
                        break;
                }
            }
            if (i % 5 == 4)
            {
                if (moduleSolved == true)
                {
                    if (alreadypressed[(i - 4) / 5] == false)
                    {
                        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
                    }
                    keyID[(i - 4) / 5].material = keyColours[6];
                    keys[(i - 4) / 5].transform.localPosition = new Vector3(0, 0, -1f);
                    keys[(i - 4) / 5].GetComponentInChildren<TextMesh>().color = new Color32(0, 0, 0, 255);
                    keys[(i - 4) / 5].GetComponentInChildren<TextMesh>().text = "0";
                    if (i == 29)
                    {
                        GetComponent<KMBombModule>().HandlePass();
                        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                    }
                }
                else
                {
                    if (alreadypressed[(i - 4) / 5] == false)
                    {
                        keys[(i - 4) / 5].transform.localPosition = new Vector3(0, 0, 0);
                        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
                    }
                    setKey((i - 4) / 5);
                }
                if (i == 29)
                {
                    i = -1;
                    pressable = true;
                    starting = false;
                    keys[6].GetComponentInChildren<TextMesh>().text = string.Empty;
                    StopCoroutine(sequence);
                }
            }
            else
            {
                for (int j = 0; j < 6; j++)
                {
                    int[] rand = new int[5];
                    if (alreadypressed[j] == false && j > (i - 4) / 5)
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            rand[k] = Random.Range(0, 6);
                        }
                        setRandomKey(j, rand[0], rand[1], rand[2], rand[3], rand[4]);
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press 0123456 [position in reading order; 0 is the black button up top] | !{0} cycle [shows border colors in reading order] | !{0} colorblind";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*colorblind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            colorblind = true;
            for (int i = 0; i < keys.Count; i++)
                setKey(i);
            yield return null;
            yield break;
        }

        if (Regex.IsMatch(command, @"^\s*cycle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            for (int i = 0; i < 6; i++)
            {
                keys[i].OnHighlight();
                yield return new WaitForSeconds(1.2f);
                keys[i].OnHighlightEnded();
                yield return new WaitForSeconds(.1f);
            }
            yield break;
        }

        var m = Regex.Match(command, @"^\s*(?:press\s*)?([0123456 ,;]+)\s*$");
        if (!m.Success)
            yield break;

        foreach (var keyToPress in m.Groups[1].Value.Where(ch => ch >= '0' && ch <= '6').Select(ch => ch == '0' ? keys[6] : keys[ch - '1']))
        {
            yield return null;
            while (!pressable)
                yield return "trycancel";
            yield return new[] { keyToPress };
        }
    }
}
