using System;
using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Quest Parent")]
public class Quest_Parent : ScriptableObject{
	[SerializeField]
	string Name;

	[SerializeField]
	[TextArea]
	string Description;
}
