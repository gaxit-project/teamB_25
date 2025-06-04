using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(PlayerBase))]
#endif

public class PlayerType1: PlayerBase
{
    public override void Attack()
    {
        Debug.Log("testAttack");
    }
}

