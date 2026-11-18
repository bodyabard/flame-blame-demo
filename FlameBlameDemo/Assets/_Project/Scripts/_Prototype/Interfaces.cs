using UnityEngine;

/// Усе, що реагує на воду: вогонь, NPC, кабель, калюжі.
public interface IWaterReceiver
{
    void ReceiveWater(Vector3 point, Vector3 normal, float amount);
}
