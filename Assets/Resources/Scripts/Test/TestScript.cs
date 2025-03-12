using System;
using System.Collections.Generic;
using System.Text;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Planet;
using Assets.Resources.Scripts.Utils;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Resources.Scripts.Test
{
    public class TestScript : MonoBehaviour
    {
        private const string dataPath = "J:/Unity3d/Projects/Galactic Frontier/Assets/Resources/Data";
        private readonly string skillDataJsonPath = dataPath + "/SkillData.json";
        private readonly string skillDataOutputPath = dataPath + "/SkillData_encrypted.bytes";
        private readonly string skillDataDecryptPath = "data/SkillData_encrypted";
        public List<SkillEntity> skillEntities = new();
        public SkillEntity firstSkillEntity;
        void Start()
        {
            // EncryptSkillData();
            // DecryptSkillData();
        }

        private void EncryptSkillData()
        {
            Debug.Log("Encrypting skill data...");
            EncryptionUtil.EncryptAndSaveFile(skillDataJsonPath, skillDataOutputPath);
        }

        private void DecryptSkillData()
        {
            Debug.Log("Decrypting skill data...");
            byte[] decryptedData = EncryptionUtil.LoadAndDecryptFile(skillDataDecryptPath);
            if (decryptedData != null)
            {
                string jsonContent = Encoding.UTF8.GetString(decryptedData);
                Debug.Log("Decrypted skill data: " + jsonContent);
                // 处理解密后的JSON内容，例如反序列化为对象
                SkillListWrapper wrapper = JsonUtility.FromJson<SkillListWrapper>(jsonContent);
                Debug.Log("Skill count: " + wrapper.skillEntities.Count + "个技能");
                firstSkillEntity = wrapper.skillEntities[0];
                Debug.Log("First skill: " + firstSkillEntity.characterName + ", name: " + firstSkillEntity.skillName.en);
                skillEntities = wrapper.skillEntities;
                foreach (var skillEntity in wrapper.skillEntities)
                {
                    Debug.Log("Skill: " + skillEntity.GetCharacterName() + ", name: " + skillEntity.GetSkillName());
                }
            }
        }
    }
}