using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class SetRelativity : MonoBehaviour
{
    public bool autoupdate = true;
    private bool initialized = false;
    public RelativitySettings relativitySettings;
    public PlanetSettings planetSettings;

    PlanetUpdator planetUpdator = new PlanetUpdator();
    RelativityUpdator relativityUpdator = new RelativityUpdator();
    
    public Vector3 lorentzTransformScale(Vector3 currScale, RelativitySettings relativitySettings)
    {
        float relativeX = (currScale.x) / Mathf.Sqrt(1 - ((relativitySettings.Velocity_Through_Space.x * relativitySettings.Velocity_Through_Space.x) / (relativitySettings.Speed_Of_Light * relativitySettings.Speed_Of_Light)));
        float relativeY = (currScale.y) / Mathf.Sqrt(1 - ((relativitySettings.Velocity_Through_Space.y * relativitySettings.Velocity_Through_Space.y) / (relativitySettings.Speed_Of_Light * relativitySettings.Speed_Of_Light)));
        float relativeZ = (currScale.z) / Mathf.Sqrt(1 - ((relativitySettings.Velocity_Through_Space.z * relativitySettings.Velocity_Through_Space.z) / (relativitySettings.Speed_Of_Light * relativitySettings.Speed_Of_Light)));

        return new Vector3(relativeX, relativeY, relativeZ);
    }

    void Initialize()
    {
        if (!initialized)
        {
            Transform[] transform = gameObject.GetComponentsInChildren<Transform>();
            int i = 0;
            foreach (Transform child in transform)
            {
                if (child.tag == "Object")
                {
                    planetSettings.Planet_Variables[i].gameObject = child.gameObject.name;
                    planetSettings.Planet_Variables[i].init_scale = child.localScale;
                    planetSettings.Planet_Variables[i].currentScale = lorentzTransformScale(planetSettings.Planet_Variables[i].init_scale, relativitySettings);
                    child.localScale = planetSettings.Planet_Variables[i].currentScale;
                    i++;
                }
            }
            initialized = true;
        }

        relativityUpdator.UpdateSettings(relativitySettings);
        planetUpdator.UpdateSettings(planetSettings);
    }

    public void UpdateSettings()
    {
        UpdateToLorentzTransform();
        Transform[] transform = gameObject.GetComponentsInChildren<Transform>();
        int i = 0;
        foreach (Transform child in transform)
        {
            if (child.tag == "Object") {
                child.localScale = planetSettings.Planet_Variables[i].currentScale;
                i++;
            }
        }
        Initialize();
    }

    public void OnRelativitySettingsUpdated()
    {
        if (autoupdate)
        {
            Initialize();
        }
    }

    public void OnPlanetSettingsUpdated()
    {
        if (autoupdate)
        {
            Initialize();
        }
    }

    public void UpdateToLorentzTransform()
    {
        for (int i = 0; i < planetSettings.Planet_Variables.Length; i++)
        {
            planetSettings.Planet_Variables[i].currentScale = lorentzTransformScale(planetSettings.Planet_Variables[i].init_scale, relativitySettings);
        }
    }

    public void Update()
    {
       
    }
}
