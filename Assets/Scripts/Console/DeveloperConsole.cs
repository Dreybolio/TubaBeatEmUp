using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DeveloperConsole : Singleton<DeveloperConsole>
{
    [SerializeField] private ConsoleCommand[] _commands;
    public ConsoleCommand[] Commands { get { return _commands; } }

    [Header("UI")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI log;

    // Vars

    private void Start()
    {
        canvas.SetActive(false);
    }

    public void ExecuteCommand(string inputValue)
    {
        string[] inputSplit = inputValue.Split(' ');

        string command = inputSplit[0];
        string[] args = inputSplit.Skip(1).ToArray();

        Log(inputValue);
        ExecuteCommand(command, args);
        inputField.text = string.Empty;
    }
    public void ExecuteCommand(string commandInput, string[] args)
    {
        foreach (var command in Commands)
        {
            if (!commandInput.Equals(command.CommandWord, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (command.Execute(args))
            {
                return;
            }
            else
            {
                Log("Error: Command did not follow input parameters");
            }
        }
    }

    public static void Log(string input)
    {
        Instance.log.text += $"{input}\n";
        Debug.Log(input);
    }

    public void Toggle()
    {
        Debug.Log("Toggling Developer Console");
        if (canvas.activeSelf)
        {
            canvas.SetActive(false);
            Time.timeScale = 1;
        }
        else
        {
            canvas.SetActive(true);
            inputField.ActivateInputField();
            Time.timeScale = 0;
        }
        print("end");
    }
}
