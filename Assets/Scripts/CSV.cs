using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSV
{
    public TextAsset text;

    public CSV(TextAsset text){
        this.text = text;
    }

    public void ReadEntityStats(Entity entity){
        string[] data = text.text.Split(new string[] { ",", "\n" }, StringSplitOptions.None);
        int tableSize = data.Length / 13;

        for (int i = 0; i < (tableSize - 1); i++){
            if (int.Parse(data[7 * (i + 1)]) == entity.EntityStatistics.Level){
                entity.EntityStatistics.HealthMax = float.Parse(data[(7 * (i + 1)) + 1]);
                entity.EntityStatistics.JumpMax = int.Parse(data[(7 * (i + 1)) + 2]);
                entity.EntityStatistics.PhysicalDamage = float.Parse(data[(7 * (i + 1)) + 3]);
                entity.EntityStatistics.PhysicalResistance = float.Parse(data[(7 * (i + 1)) + 4]);
                entity.EntityStatistics.SpellDamage = float.Parse(data[(7 * (i + 1)) + 5]);
                entity.EntityStatistics.SpellResistance = float.Parse(data[(7 * (i + 1)) + 6]);

                break;
            }
        }
    }

    public void SetEntityLevel(Entity entity, int level){
        if (level <= 20){
            entity.EntityStatistics.Level = level;

            ReadEntityStats(entity);
        }
        else{
            Debug.Log("Entity level too high");
        }
    }


}
