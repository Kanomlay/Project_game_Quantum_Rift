using System.Collections.Generic;
using UnityEngine;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Data & UI Setup")]
    public List<CharacterData> allCharacters; // ลิสต์สำหรับโยน Data ตัวละครใส่เข้ามา
    public GameObject characterBoxPrefab;     // แม่พิมพ์กล่อง (Prefab)
    public Transform characterContainer;      // ตะกร้าที่ใส่ Horizontal Layout Group ไว้

    void Start()
    {
        // สั่งให้สร้างกล่องทันทีที่เกมเริ่ม
        GenerateCharacterBoxes(); 
    }

    public void GenerateCharacterBoxes()
    {
        // 1. เคลียร์กล่องเก่าทิ้ง (เพื่อป้องกันการสร้างซ้ำเวลาเรียกใหม่)
        foreach (Transform child in characterContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. วนลูปตามจำนวน Data ที่โยนเข้ามา
        foreach (CharacterData data in allCharacters)
        {
            // เสกกล่องใหม่ลงในตะกร้า
            GameObject newBox = Instantiate(characterBoxPrefab, characterContainer);
            
            // ดึงสคริปต์บนกล่อง แล้วส่ง Data ไปให้มันอัปเดตหน้าตา
            CharacterBoxUI boxUI = newBox.GetComponent<CharacterBoxUI>();
            if (boxUI != null)
            {
                boxUI.SetupBox(data);
            }
        }
    }
}