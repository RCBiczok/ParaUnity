using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

public class ThreadUtil : MonoBehaviour
{
	private static ThreadUtil _current;

	public static ThreadUtil Current {
		get {
			if (_current == null && Application.isPlaying) {

				var g = GameObject.Find ("ThreadUtil");
				if (g == null) {
					g = new GameObject ("ThreadUtil");
					g.hideFlags = HideFlags.HideAndDontSave;
				}

				_current = g.GetComponent<ThreadUtil> () ?? g.AddComponent<ThreadUtil> ();
			}

			return _current;
		}
	}

	void Awake ()
	{
		if (_current != null && _current != this) {
			DestroyImmediate (gameObject);
		} else {
			_current = this;
		}
	}

	private List<Action> _actions = new List<Action> ();

	public class DelayedQueueItem
	{
		public float time;
		public Action action;
		public string name;
	}

	private List<DelayedQueueItem> _delayed = new  List<DelayedQueueItem> ();

	public static void QueueOnMainThread (Action action, float time, string name)
	{
		lock (Current._delayed) {
			if (Current._delayed.Any (d => d.name == name))
				return;
			QueueOnMainThread (action, time);
		}
	}

	public static void QueueOnMainThread (Action action, string name)
	{
		QueueOnMainThread (action, 0, name);
	}

	public static void QueueOnMainThread (Action action, float time)
	{
		if (time != 0) {
			lock (Current._delayed) {
				Current._delayed.Add (new DelayedQueueItem { time = Time.time + time, action = action });
			}
		} else {
			lock (Current._actions) {
				Current._actions.Add (action);
			}
		}
	}

	public static void QueueOnMainThread (Action action)
	{
		lock (Current._actions) {
			Current._actions.Add (action);
		}
	}

	public static void RunAsync (Action a)
	{
		var t = new Thread (RunAction);
		t.Priority = System.Threading.ThreadPriority.Normal;
		t.Start (a);
	}

	private static void RunAction (object action)
	{
		((Action)action) ();
	}


	Action[] toBeRun = new Action[1000];
	DelayedQueueItem[] toBeDelayed = new DelayedQueueItem[1000];

	void Update ()
	{
		try {
			var actions = 0;
			var delayedCount = 0;
			//Process the non-delayed actions
			lock (_actions) {
				for (var i = 0; i < _actions.Count; i++) {
					toBeRun [actions++] = _actions [i];
					if (actions == 999)
						break;
				}
				_actions.Clear ();
			}
			for (var i = 0; i < actions; i++) {
				var a = toBeRun [i];
				try {
					a ();
				} catch (Exception e) {
					Debug.LogError ("Queued Exception: " + e.ToString ());
				}
			}
			lock (_delayed) {
				for (var i = 0; i < _delayed.Count; i++) {
					var d = _delayed [i];
					if (d.time < Time.time) {
						toBeDelayed [delayedCount++] = d;
						if (delayedCount == 999)
							break;
					}
				} 
			}
			for (var i = 0; i < delayedCount; i++) {
				var delayed = toBeDelayed [i];
				lock (_delayed) {
					_delayed.Remove (delayed);
				}
				try {
					delayed.action ();
				} catch (Exception e) {
					Debug.LogError ("Delayed Exception:" + e.ToString ());
				}
			}

		} catch (Exception e) {
			Debug.LogError ("ThreadUtil Error " + e.ToString ());
		}
	}
}
