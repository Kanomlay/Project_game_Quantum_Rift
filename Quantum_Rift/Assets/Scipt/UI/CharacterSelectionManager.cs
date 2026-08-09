using System.Collections.Generic;
using UnityEngine;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Data & UI Setup")]
    public List<CharacterData> allCharacters;
    public GameObject characterBoxPrefab;
    public Transform characterContainer;

    // เก็บกล่องที่เสกออกมาทั้งหมดไว้ใน List เพื่อสั่งเปิด/ปิด
    private List<GameObject> spawnedBoxes = new List<GameObject>();
    private int currentIndex = 0; // ตัวบอกว่าตอนนี้กำลังโชว์กล่องที่เท่าไหร่

    void Start()
    {
        GenerateCharacterBoxes(); 
    }

    public void GenerateCharacterBoxes()
    {
        // 1. เคลียร์กล่องเก่าทิ้ง
        foreach (Transform child in characterContainer)
        {
            Destroy(child.gameObject);
        }
        spawnedBoxes.Clear();

        // 2. วนลูปเสกกล่องตามจำนวน Data
        for (int i = 0; i < allCharacters.Count; i++)
        {
            GameObject newBox = Instantiate(characterBoxPrefab, characterContainer);
            
            CharacterBoxUI boxUI = newBox.GetComponent<CharacterBoxUI>();
            if (boxUI != null)
            {
                boxUI.SetupBox(allCharacters[i]);
            }

            spawnedBoxes.Add(newBox); // เก็บกล่องใส่ List ไว้

            // 3. ถ้าเป็นตัวแรก (ลำดับที่ 0) ให้เปิดตาไว้ นอกนั้นสั่งซ่อนให้หมด
            if (i == 0)
            {
                newBox.SetActive(true);
            }
            else
            {
                newBox.SetActive(false);
            }
        }

        currentIndex = 0; // รีเซ็ตตำแหน่งกลับมาตัวแรก
    }

    // ฟังก์ชันสำหรับปุ่มลูกศรขวา (>)
    public void NextCharacter()
    {
        if (spawnedBoxes.Count == 0) return;

        // ซ่อนกล่องปัจจุบัน
        spawnedBoxes[currentIndex].SetActive(false);

        // เลื่อนไปตัวถัดไป (ถ้าเกินตัวสุดท้าย ให้วนกลับมาตัวที่ 1)
        currentIndex++;
        if (currentIndex >= spawnedBoxes.Count)
        {
            currentIndex = 0;
        }

        // เปิดกล่องใหม่
        spawnedBoxes[currentIndex].SetActive(true);
    }

    // ฟังก์ชันสำหรับปุ่มลูกศรซ้าย (<)
    public void PreviousCharacter()
    {
        if (spawnedBoxes.Count == 0) return;

        // ซ่อนกล่องปัจจุบัน
        spawnedBoxes[currentIndex].SetActive(false);

        // เลื่อนถอยหลัง (ถ้าถอยเลยตัวที่ 1 ให้วนไปตัวสุดท้าย)
        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = spawnedBoxes.Count - 1;
        }

        // เปิดกล่องใหม่
        spawnedBoxes[currentIndex].SetActive(true);
    }
}