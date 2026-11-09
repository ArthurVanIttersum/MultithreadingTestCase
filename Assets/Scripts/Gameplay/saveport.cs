using System.IO.Ports;
using UnityEngine;

[CreateAssetMenu(fileName = "saveport", menuName = "Scriptable Objects/saveport")]
public class saveport : ScriptableObject
{
    public SerialPort savedPort;
}
