using DevTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersonasManager : GameSystemComponent
{
    public static PersonasManager Instance { get; private set; }
    
    private bool isEditorScene = false;

    public string CurrentPersonaName
    {
        get
        {
            Persona currentPersona = CurrentPersona;
            PersonaInfo currentPersonaInfo = CurrentPersonaInfo;

            if (currentPersonaInfo == null || currentPersona == null)
                return "None";

            if (currentPersonaInfo.personaValue <= 0)
                return "None";

            return currentPersona.personaName;
        }
    }
    public int CurrentPersonaIndex
    {
        get
        {
            if (Personas.IsNullOrEmpty())
                return -1;

            if (isEditorScene)
                return 0;

            int highestValue = int.MinValue;
            int highestIndex = 0;
            List<PersonaInfo> personaInfos = PersonaInfos;

            if (personaInfos.IsNullOrEmpty())
                return -1;

            for (int i = 0; i < personaInfos.Count; i++)
            {
                if (personaInfos[i].personaValue > highestValue)
                {
                    highestValue = personaInfos[i].personaValue;
                    highestIndex = i;
                }
            }

            return highestIndex;
        }
    }

    public Persona CurrentPersona
    {
        get
        {
            int index = CurrentPersonaIndex;

            if (index < 0)
                return null;

            return Personas[index];
        }
    }

    public PersonaInfo CurrentPersonaInfo
    {
        get
        {
            int index = CurrentPersonaIndex;

            if (index < 0)
                return null;

            if(PersonaInfos.IsNullOrEmpty())
                return null;

            return PersonaInfos[index];
        }
    }

    public List<Persona> Personas
    {
        get
        {
            if (isEditorScene)
                return DevToolsManager.ActiveUnit.personas;

            return PlayerController.Instance.ActiveUnit.data.personas;
        }
    }

    public List<PersonaInfo> PersonaInfos
    {
        get
        {
            if (isEditorScene)
            {
                Debug.LogError("Trying to get PersonaInfos in editor scene!");
                return new List<PersonaInfo>();
            }

            return PlayerController.Instance.ActiveUnit.info.personas;
        }
    }

    #region Public Methods
    public void CreatePersonas(UnitInfo unitInfo)
    {
        if (Personas.IsNullOrEmpty())
        {
            Debug.LogError("Personas list is empty.");
            return;
        }

        for (int i = 0; i < Personas.Count; i++)
        {
            Persona persona = Personas[i];

            unitInfo.personas.Add(new PersonaInfo()
            {
                personaID = persona.personaID,
                personaValue = 0
            });
        }
    }

    public void ChangePersonaValue(string personaID, int value)
    {
        PersonaInfo personaInfo = GetPersonaInfoByID(personaID);

        if(personaInfo == null)
        {
            Debug.LogError($"PersonaInfo {personaID} not found.");
            return;
        }

        personaInfo.personaValue += value;
        personaInfo.personaValue.ClampInt(0, 100);
    }
    #endregion

    #region Getters
    public string GetPersonaNameByID(string personaID)
    {
        Persona persona = GetPersonaByID(personaID);

        if (persona == null)
            return "Persona not found";

        return persona.personaName;
    }

    public string GetPersonaDescriptionByID(string personaID)
    {
        Persona persona = GetPersonaByID(personaID);

        if (persona == null)
            return "Persona not found";

        return persona.personaDescription;
    }

    public Persona GetPersonaByID(string personaID)
    {
        return Personas.Find(x => x.personaID == personaID);
    }

    public PersonaInfo GetPersonaInfoByID(string personaID)
    {
        return PersonaInfos.Find(x => x.personaID == personaID);
    }
    #endregion

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        isEditorScene = SceneManager.GetActiveScene().name.Contains("editor", System.StringComparison.OrdinalIgnoreCase);
    }
}
