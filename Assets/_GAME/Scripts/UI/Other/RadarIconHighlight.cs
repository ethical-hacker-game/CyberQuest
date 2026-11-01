using Sirenix.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RadarIconHighlight : UICursorHint
{
    [SerializeField] private Text textComponent;
    [SerializeField] private Image icon;

    private Persona persona;

    private string PersonaID => persona.personaID;

    protected override void Start()
    {
        base.Start();
        persona = PersonasManager.Instance.GetPersonaByID(textComponent.text);

        if (persona == null) 
            return;

        icon.sprite = persona.GetPersonaPicture();
        textComponent.text = persona.personaName;
        HintText = $"<b>{persona.personaName}</b>\n" +
            $"<i>click to read more...</i>";

        OnMouseDown.AddListener(() =>
        {
            ContentUI.SetupContentUI(persona.personaName, persona.GetPersonaPicture(), persona.personaDescription, "Back");
            ContentUI.SetContentImageHeight(100);
            ContentUI.ButtonEvents[0].AddListener(GameplayUIManager.ShowQuestsUI);
        });
        
    }
}
