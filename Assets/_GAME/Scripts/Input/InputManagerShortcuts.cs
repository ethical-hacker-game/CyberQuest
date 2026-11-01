using System.Linq;
using System.Collections.Generic;
using UnityEngine;

//SHORTCUTS
public partial class InputManager
{
    [SerializeField] private KeyCode back = KeyCode.Escape;

    [SerializeField]
    private KeyCode[] numActions =
        new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
            KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7 };

    private void HandleCustomKeys()
    {
        if (Input.GetKeyDown(back))
            CommonUISolver.CurrentUIScreen?.Back();

        //for (int i = 0; i < numActions.Length; i++)
        //    if (Input.GetKeyDown(numActions[i]))
        //        GameplayUIManager.Instance.CombatUI.SetActionByIndex(i);
    }

    private void HandleMainMenuKeys()
    {
        if (Input.GetKeyDown(back))
            CommonUISolver.CurrentUIScreen?.Back();
    }
}
