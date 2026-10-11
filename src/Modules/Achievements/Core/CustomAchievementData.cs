namespace TONX.Modules.Achievements.Core;

public sealed class AchievementRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string UnlockedAt { get; set; } = "";
    public bool Unlocked { get; set; }
    public int Progress { get; set; }
    public int Required { get; set; }
}

public sealed class CustomAchievementData
{
    private readonly object _lock = new();

    private readonly Dictionary<int, AchievementRecord> _records = new();
    private readonly Dictionary<int, AchievementRecord> _dirty = new();
    private readonly HashSet<int> _announced = new();

    public string FriendCode { get; }
    public bool IsLocal { get; }

    public bool HasData { get; private set; }

    public DateTime LoadedAt { get; private set; }

    public CustomAchievementData(string friendCode, bool isLocal = false)
    {
        FriendCode = NormalizeFriendCode(friendCode);
        IsLocal = isLocal;
    }

    public static string NormalizeFriendCode(string friendCode)
        => (friendCode ?? "").Trim().ToLowerInvariant();

    public IReadOnlyList<AchievementRecord> Records
    {
        get { lock (_lock) return _records.Values.ToList(); }
    }

    public bool HasPendingChanges
    {
        get { lock (_lock) return _dirty.Count > 0; }
    }

    public int UnlockedCount
    {
        get { lock (_lock) return _records.Values.Count(r => IsUnlocked(r)); }
    }

    public int TotalCount => AchievementRegistry.GetAll().Count;

    public static bool IsUnlocked(AchievementRecord record)
        => record != null
           && (record.Unlocked || (record.Required > 0 && record.Progress >= record.Required));

    public bool Has(int id) => IsUnlocked(Get(id));

    public AchievementRecord Get(int id)
    {
        lock (_lock) return _records.TryGetValue(id, out var r) ? r : null;
    }

    public int GetProgress(int id)
    {
        lock (_lock) return _records.TryGetValue(id, out var r) ? r.Progress : 0;
    }

    internal void MarkLoaded()
    {
        lock (_lock)
        {
            HasData = true;
            LoadedAt = DateTime.UtcNow;
        }
    }

    public void MergeServer(IEnumerable<AchievementRecord> records)
    {
        if (records == null) return;

        lock (_lock)
        {
            foreach (var incoming in records)
            {
                if (incoming == null || incoming.Id <= 0) continue;

                if (_records.TryGetValue(incoming.Id, out var existing))
                {
                    existing.Progress = Math.Max(existing.Progress, incoming.Progress);
                    existing.Required = incoming.Required > 0 ? incoming.Required : existing.Required;
                    if (incoming.Unlocked)
                    {
                        existing.Unlocked = true;
                        if (string.IsNullOrEmpty(existing.UnlockedAt)) existing.UnlockedAt = incoming.UnlockedAt;
                    }
                    if (string.IsNullOrEmpty(existing.Name) && !string.IsNullOrEmpty(incoming.Name))
                        existing.Name = incoming.Name;
                    if (string.IsNullOrEmpty(existing.Description) && !string.IsNullOrEmpty(incoming.Description))
                        existing.Description = incoming.Description;
                }
                else
                {
                    _records[incoming.Id] = new AchievementRecord
                    {
                        Id = incoming.Id,
                        Name = incoming.Name ?? "",
                        Description = incoming.Description ?? "",
                        UnlockedAt = incoming.UnlockedAt ?? "",
                        Unlocked = incoming.Unlocked,
                        Progress = incoming.Progress,
                        Required = incoming.Required,
                    };
                }
            }

            HasData = true;
            LoadedAt = DateTime.UtcNow;
        }
    }

    public bool ApplyUnlock(AchievementBase def)
    {
        if (def == null) return false;

        lock (_lock)
        {
            var record = EnsureRecord(def.Id);
            if (IsUnlocked(record)) return false;

            record.Unlocked = true;
            record.Progress = def.RequiredValue;
            record.Required = def.RequiredValue;
            record.Name = def.Name;
            record.Description = def.Description;
            record.UnlockedAt = DateTime.UtcNow.ToString("o");

            _dirty[def.Id] = Clone(record);
            Logger.Info($"{FriendCode} unlocked [{def.Id}]{def.Name}", "CustomAchievementData");
            return true;
        }
    }

    public bool ApplyProgress(AchievementBase def, int value)
    {
        if (def == null || value <= 0) return false;

        lock (_lock)
        {
            var record = EnsureRecord(def.Id);
            if (IsUnlocked(record)) return false;
            if (value <= record.Progress) return false;

            record.Progress = Math.Min(value, def.RequiredValue);
            record.Required = def.RequiredValue;
            if (string.IsNullOrEmpty(record.Name)) record.Name = def.Name;
            if (string.IsNullOrEmpty(record.Description)) record.Description = def.Description;

            var reached = IsUnlocked(record);
            if (reached)
            {
                record.Unlocked = true;
                if (string.IsNullOrEmpty(record.UnlockedAt)) record.UnlockedAt = DateTime.UtcNow.ToString("o");
                Logger.Info($"{FriendCode} unlocked [{def.Id}]{def.Name}", "CustomAchievementData");
            }

            _dirty[def.Id] = Clone(record);
            return reached;
        }
    }

    public List<AchievementRecord> TakeUnannounced()
    {
        lock (_lock)
        {
            var result = _records.Values
                .Where(IsUnlocked)
                .Where(r => !_announced.Contains(r.Id))
                .Select(Clone)
                .ToList();

            foreach (var record in result) _announced.Add(record.Id);
            return result;
        }
    }

    public List<AchievementRecord> PeekDirty()
    {
        lock (_lock) return _dirty.Values.Select(Clone).ToList();
    }

    public void CommitDirty()
    {
        lock (_lock) _dirty.Clear();
    }

    public void DiscardDirty()
    {
        lock (_lock) _dirty.Clear();
    }

    private AchievementRecord EnsureRecord(int id)
    {
        if (_records.TryGetValue(id, out var record)) return record;

        record = new AchievementRecord { Id = id };
        _records[id] = record;
        return record;
    }

    private static AchievementRecord Clone(AchievementRecord record) => new()
    {
        Id = record.Id,
        Name = record.Name,
        Description = record.Description,
        UnlockedAt = record.UnlockedAt,
        Unlocked = record.Unlocked,
        Progress = record.Progress,
        Required = record.Required,
    };
}
