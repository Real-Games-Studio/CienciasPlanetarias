using System.Collections.Generic;
using UnityEngine;

public class CanvasScreenPais : CanvasScreen
{
    public struct ContinentData
    {
        public string continentName;
        public GameObject backgroundImage;
        public List<string> countryScreens;
    }
}
