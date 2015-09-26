using UnityEngine;	
using System.Collections;
using System.Collections.Generic;
using Leap;

using System;
using System.IO;

public class RiggedHandController : MonoBehaviour {

	Dictionary<int, GameObject> m_hands = new Dictionary<int, GameObject>();
	Controller m_leapController;
	SkeletalHandController m_skeletalDrawer;
	PauseManager m_pauseManager;

	// Use this for initialization
	void Start () {
		m_skeletalDrawer = GetComponent<SkeletalHandController>();

		try {
			m_leapController = new Controller();
		} catch (Exception e) {
			Debug.Log(e);
		}
		m_pauseManager = GameObject.Find("PauseManager").GetComponent<PauseManager>();
	}

	Hand FindFrontLeftHand(Frame f) {
		Hand h = null;
		float compVal = -float.MaxValue;
		for (int i = 0; i < f.Hands.Count; ++i) {
			if (f.Hands[i].IsLeft && f.Hands[i].PalmPosition.ToUnityScaled().z > compVal) {
				compVal = f.Hands[i].PalmPosition.ToUnityScaled().z;
				h = f.Hands[i];
			}
		}
		return h;
	}
	
	Hand FindFrontRightHand(Frame f) {
		Hand h = null;
		float compVal = -float.MaxValue;
		for (int i = 0; i < f.Hands.Count; ++i) {
			if (f.Hands[i].IsRight && f.Hands[i].PalmPosition.ToUnityScaled().z > compVal) {
				compVal = f.Hands[i].PalmPosition.ToUnityScaled().z;
				h = f.Hands[i];
			}
		}
		return h;
	}

	public void ShowHands(bool shouldShow) {
		foreach(KeyValuePair<int, GameObject> h in m_hands) {
			h.Value.GetComponent<LeapRiggedHand>().Draw(shouldShow);
		}

	}
	
	// Update is called once per frame
	void Update () {
		if (m_leapController == null) return;

		if (m_pauseManager.IsPaused()) return;
		
		// mark exising hands as stale.
		foreach(KeyValuePair<int, GameObject> h in m_hands) {
			h.Value.GetComponent<LeapRiggedHand>().m_stale = true;
		}

		Frame f = m_leapController.Frame();

		// see what hands the leap sees and mark matching hands as not stale.
		for(int i = 0; i < f.Hands.Count; ++i) {
			
			GameObject hand;
			
			// see if hand existed before
			if (m_hands.TryGetValue(f.Hands[i].Id, out hand)) {
				
				// HACK to get around the id not resetting with handedness reset bug.
				if (f.Hands[i].IsRight != hand.GetComponent<LeapRiggedHand>().IsRight()) {
					Debug.LogError("handedness not matching");
					continue;
				}
				// if it did then just update its position and joint positions.
				hand.GetComponent<LeapRiggedHand>().UpdateRig(f.Hands[i]);
			} else {
				// else create new hand
				hand = Instantiate(Resources.Load("Prefabs/LeapRiggedHand")) as GameObject;
				hand.GetComponent<LeapRiggedHand>().InitializeHand(f.Hands[i].IsRight);
				// push it into the dictionary.
				m_hands.Add(f.Hands[i].Id, hand);
			}
			
		}

		// clear out stale hands.
		List<int> staleIDs = new List<int>();
		foreach(KeyValuePair<int, GameObject> h in m_hands) {
			if (h.Value.GetComponent<LeapRiggedHand>().m_stale) {
				Destroy(h.Value);
				// set for removal from dictionary.
				staleIDs.Add(h.Key);
			}
		}
		foreach(int id in staleIDs) {
			m_hands.Remove(id);
		}
		
	}
}
