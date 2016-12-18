using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Logic : MonoBehaviour {

	public KMSelectable[] buttons;
	public TextMesh[] info;
	public bool ansA = false, ansB = false;
	public bool[] tog;
	public int[] port, indc, batt, keys, num;
	public string serl;

	void Start(){
		GetComponent<KMBombModule>().OnActivate += Init;
	}

	void Awake () {
		buttons [0].OnInteract += delegate() {
			HandlePress (0);
			return false;
		};
		buttons [1].OnInteract += delegate() {
			HandlePress (1);
			return false;
		};
		buttons [2].OnInteract += delegate() {
			checkAns ();
			return false;
		};
	}

	void Init() {

		for (int i = 0; i < 6; i++) {
			num [i] = Random.Range (0, 25) + 65;
			info [i].text =char.ConvertFromUtf32(num[i]);
		}
		List<string> responsePORT = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_PORTS, null);
		foreach (string response in responsePORT) {
			Debug.Log (response);
			Dictionary<string, string[]> responseDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(response);
			foreach (string s in responseDict["presentPorts"])
			{
				if (string.Compare (s, "DVI", true) == 0) port [0]++;
				else if (string.Compare (s, "Parallel", true) == 0) port [1]++;
				else if (string.Compare (s, "PS2", true) == 0) port [2]++;
				else if (string.Compare (s, "RJ45", true) == 0) port [3]++;
				else if (string.Compare (s, "Serial", true) == 0) port [4]++;
				else if (string.Compare (s, "StereoRCA", true) == 0) port [5]++;
			}
		}
		List<string> responseBATT = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_BATTERIES, null);
		foreach (string response in responseBATT) {
			Debug.Log (response);
			Dictionary<string, int> responseDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(response);
			batt[0] += responseDict["numbatteries"];
		}
		List<string> responseINDC = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_INDICATOR, null);
		foreach (string response in responseINDC) {
			Debug.Log (response);
			Dictionary<string, string> responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
			string label = responseDict["label"];
			bool active = responseDict["on"].Equals("True");
			if (active) {
				if (string.Compare (label, "CLR") == 0) indc [0]++;
				else if (string.Compare (label, "FRK") == 0) indc [1]++;
				else if (string.Compare (label, "BOB") == 0) indc [2]++;
				else if (string.Compare (label, "CAR") == 0) indc [3]++;
				else if (string.Compare (label, "SIG") == 0) indc [4]++;
				else if (string.Compare (label, "MSA") == 0) indc [5]++;
				else if (string.Compare (label, "IND") == 0) indc [6]++;
				indc [7]++;
			}
		}
		List<string> responseSERL = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null);
		foreach (string response in responseSERL) {
			Debug.Log (response);
			Dictionary<string, string> responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
			serl = responseDict["serial"];
		}
		for (int i = 0; i < 6; i++)
		{
			if (port[i] != 0)
				port[6]++;
		} 
		generateAns ();
		Debug.Log ("[Logic] ready! Answers generated!");
	}

	void generateAns(){
		if (batt [0] > 2) keys [0] = 1; if (port [4] != 0) keys [1] = 1;
		if (port [1] != 0) keys [2] = 1; /*if (port [4] != 0) keys [3] = 1; SERL*/
		/*if (port [4] != 0) keys [4] = 1; SERL*/ if (port [5] != 0) keys [5] = 1;
		if (indc [0] != 0) keys [6] = 1; if (indc [6] != 0) keys [7] = 1;
		if (batt [0] == 0) keys [8] = 1; if (indc [5] != 0) keys [9] = 1;
		/*if (port [4] != 0) keys [10] = 1; if (port [4] != 0) keys [11] = 1; SERL*/
		if (indc [1] != 0) keys [12] = 1; if (batt [0] == 1) keys [13] = 1;
		if (batt [0] == 0) keys [14] = 1; if (port [3] != 0) keys [15] = 1;
		if (port [0] != 0) keys [16] = 1; if (batt [0] > 5) keys [17] = 1;
		if (indc [3] != 0 && indc [4] != 0) keys [18] = 1; if (batt [0] > 1 && port [2] != 0) keys [19] = 1;
		if (port [4] != 0 && port [1] != 0) keys [20] = 1; if (indc [2] != 0) keys [21] = 1;
		/*if (port [4] != 0) keys [22] = 1; SERL*/ if (port [6] > 3) keys [23] = 1;
		if (indc [7] == 0) keys [24] = 1; if (port [4] != 0 && port [3] != 0) keys [25] = 1;

		for (int i = 0; i < 6; i++) {
			if (serl [i] == 'A' || serl [i] == 'E' || serl [i] == 'I' || serl [i] == 'O' || serl [i] == 'U') keys [3] = 1;
		}
		if (keys [3] == 0) keys [4] = 1;
		if (serl [0] < 65 && serl [1] < 65 && serl [2] < 65 && serl [3] < 65 && serl [4] < 65 && serl [5] < 65) keys [22] = 1;
		if ((serl [5] - 48) % 2 == 0) keys [11] = 1; else keys[10] = 1;

		if (keys [num [0] - 65] == 1 && keys [num [1] - 65] == 1 && keys [num [2] - 65] == 1) ansA = true;
		if (keys [num [3] - 65] == 1 || keys [num [4] - 65] == 1 || keys [num [5] - 65] == 1) ansB = true;

	}

	void HandlePress(int mode)
	{
		if (tog [mode] == false) {
			buttons[mode].GetComponent<MeshRenderer>().material.color = Color.green;
			info[mode+6].text = "T";
			tog [mode] = true;
		} 
		else {
			buttons[mode].GetComponent<MeshRenderer>().material.color = Color.red;
			info[mode+6].text = "F";
			tog [mode] = false;
		}
	}

	void checkAns(){
		if (tog [0] == ansA && tog [1] == ansB)
			GetComponent<KMBombModule>().HandlePass ();
		else
			GetComponent<KMBombModule>().HandleStrike ();
	}
}
