// ของในแมพที่ผู้เล่นตีแตกได้ (กล่องทำลายได้ / กำแพงในห้อง) อาวุธระยะประชิดกับกระสุนหาผ่าน interface นี้
public interface IBreakable
{
    bool IsBroken { get; }
    void TakeDamage(float amount);
}
