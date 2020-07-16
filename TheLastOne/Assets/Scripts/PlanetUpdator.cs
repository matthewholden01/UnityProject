using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class PlanetUpdator 
{
    PlanetSettings planetSettings;
    public void UpdateSettings(PlanetSettings planetSettings)
    {
        this.planetSettings = planetSettings;

        for(int i = 0; i < planetSettings.Planet_Variables.Length; i++)
        {
            this.planetSettings.Planet_Variables[i].currentScale = planetSettings.Planet_Variables[i].currentScale;
        }
    }
}
