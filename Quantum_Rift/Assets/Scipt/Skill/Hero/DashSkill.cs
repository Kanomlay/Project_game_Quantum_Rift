using UnityEngine;

[CreateAssetMenu(fileName = "NewDashSkill", menuName = "Game Data/Skills/Dash Skill")]
public class DashSkill : SkillData
{
    [Header("ตั้งค่าเฉพาะของสกิลพุ่ง")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;

    public override void ActivateSkill(GameObject player)
    {
        // 1. ดึงสคริปต์เดินของตัวละครมา
        PlayerMovement movementScript = player.GetComponent<PlayerMovement>();
        
        if (movementScript != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0; 
            Vector2 dashDirection = (mousePos - player.transform.position).normalized;
            movementScript.StartDash(dashDirection, dashSpeed, dashDuration);
        }
        
    }
}