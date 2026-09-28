using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class BenchUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetEmptySlot(EntityManager entityManager, Entity benchEntity, out int index)
        {
            index = -1;
            var slots = entityManager.GetBuffer<BenchSeatElement>(benchEntity);

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Seat == Entity.Null)
                {
                    index = i;
                    return slots[i].Seat;
                }
            }

            return Entity.Null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EnterAnySeat(EntityManager entityManager, Entity benchEntity, Entity npcEntity, out int index)
        {
            var slots = entityManager.GetBuffer<BenchSeatElement>(benchEntity);

            return EnterAnySeat(npcEntity, ref slots, out index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EnterAnySeat(Entity npcEntity, ref DynamicBuffer<BenchSeatElement> benchSlots, out int index)
        {
            index = -1;

            for (int i = 0; i < benchSlots.Length; i++)
            {
                if (benchSlots[i].Seat == Entity.Null)
                {
                    var slot = benchSlots[i];
                    slot.Seat = npcEntity;
                    benchSlots[i] = slot;
                    index = i;
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetOut(Entity npcEntity, ref DynamicBuffer<BenchSeatElement> benchSlots)
        {
            for (int i = 0; i < benchSlots.Length; i++)
            {
                if (benchSlots[i].Seat == npcEntity)
                {
                    var slot = benchSlots[i];
                    slot.Seat = Entity.Null;
                    benchSlots[i] = slot;
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetSeatPosition(int seatNumber, NodeSeatSettingsComponent nodeSeatSettingsComponent)
        {
            var seatPosition = PedestrianBenchPositionHelper.GetSeatPosition(seatNumber, nodeSeatSettingsComponent.SeatsCount, nodeSeatSettingsComponent.SeatOffset, nodeSeatSettingsComponent.InitialPosition, nodeSeatSettingsComponent.BaseOffset, nodeSeatSettingsComponent.InitialRotation);

            seatPosition.y = nodeSeatSettingsComponent.SeatHeight;

            return seatPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetEnterPosition(int seatNumber, NodeSeatSettingsComponent nodeSeatSettingsComponent)
        {
            var seatPosition = PedestrianBenchPositionHelper.GetSeatPosition(seatNumber, nodeSeatSettingsComponent.SeatsCount, nodeSeatSettingsComponent.SeatOffset, nodeSeatSettingsComponent.InitialPosition, nodeSeatSettingsComponent.BaseOffset, nodeSeatSettingsComponent.InitialRotation);

            seatPosition += math.mul(nodeSeatSettingsComponent.InitialRotation, new float3(0, 0, nodeSeatSettingsComponent.EnterSeatOffset));

            return seatPosition;
        }
    }
}