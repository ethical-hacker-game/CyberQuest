using UnityEngine;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public partial class InputManager : GameSystemComponent
{
    public static InputManager Instance;

    [SerializeField] private int mouseBorder = 20;

    [Header("Keys")]
    [SerializeField] private KeyCode horizontalLeft = KeyCode.A;
    [SerializeField] private KeyCode horizontalRight = KeyCode.D;
    [SerializeField] private KeyCode verticalUp = KeyCode.W;
    [SerializeField] private KeyCode verticalDown = KeyCode.S;

    private bool isMenuScene;

    public static Vector3 mousePosition => Input.mousePosition;
    public static float mouseScrollDelta => Input.mouseScrollDelta.y;
    public static bool pointerOverUI => UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    public static Vector2 mouseAxes => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    public static Vector2 steeringAxes
    {
        get
        {
            if (Application.isFocused == false)
                return Vector2.zero;

            Vector2 axes = new Vector2();

            if (Input.GetKey(Instance.horizontalLeft))
                axes.x = -1;
            else if (Input.GetKey(Instance.horizontalRight))
                axes.x = 1;

            if (Input.GetKey(Instance.verticalUp))
                axes.y = 1;
            else if (Input.GetKey(Instance.verticalDown))
                axes.y = -1;

            if (pointerOverUI)
                return axes;

            if (mousePosition.x.IsValueInRange(0, Screen.width) == false
                || mousePosition.y.IsValueInRange(0, Screen.height) == false)
                return axes;

            if (mousePosition.x < Instance.mouseBorder)
                axes.x = -1;
            else if (mousePosition.x + Instance.mouseBorder > Screen.width)
                axes.x = 1;

            if (mousePosition.y < Instance.mouseBorder)
                axes.y = -1;
            else if (mousePosition.y + Instance.mouseBorder > Screen.height)
                axes.y = 1;

            return axes;
        }
    }


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        //isMenuScene = SceneManager.GetActiveScene().buildIndex == 0;
        isMenuScene = SceneManager.GetActiveScene().name.Contains("menu", StringComparison.OrdinalIgnoreCase);
    }

    private void Update()
    {
        if (isMenuScene)
        {
            HandleMainMenuKeys();
            return;
        }

        HandleCustomKeys();
    }
}

