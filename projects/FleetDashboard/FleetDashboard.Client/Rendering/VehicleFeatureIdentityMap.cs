namespace FleetDashboard.Client.Rendering;


public sealed class VehicleFeatureIdentityMap(IEnumerable<int> vehicleIds)
{
    public IReadOnlyCollection<int> VehicleIds => _objectIdsByVehicleId.Keys;

    public void Add(int vehicleId, int objectId)
    {
        _objectIdsByVehicleId.Add(vehicleId, objectId);
    }

    public bool Contains(int vehicleId)
    {
        return _objectIdsByVehicleId.ContainsKey(vehicleId);
    }

    public int GetObjectId(int vehicleId)
    {
        return _objectIdsByVehicleId[vehicleId];
    }

    public void Remove(int vehicleId)
    {
        _objectIdsByVehicleId.Remove(vehicleId);
    }

    public bool TryGetVehicleId(int objectId, out int vehicleId)
    {
        foreach ((int candidateVehicleId, int candidateObjectId) in _objectIdsByVehicleId)
        {
            if (candidateObjectId != objectId) continue;

            vehicleId = candidateVehicleId;
            return true;
        }

        vehicleId = default;
        return false;
    }

    private readonly Dictionary<int, int> _objectIdsByVehicleId = vehicleIds.ToDictionary(id => id);
}
