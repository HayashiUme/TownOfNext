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

    public AchievementRecord Clone() => new()
    {
        Id = Id,
        Name = Name,
        Description = Description,
        UnlockedAt = UnlockedAt,
        Unlocked = Unlocked,
        Progress = Progress,
        Required = Required,
    };
}

public sealed class CustomAchievementData
{
    private readonly object _lock = new();
    private readonly Dictionary<int, AchievementRecord> _records = new();
    private readonly HashSet<int> _pendingIds = new();
    private HashSet<int> _roundStartUnlocked = new();
    private bool _hasData;
    private DateTime _loadedAt;
    private bool _hasRoundSnapshot;
    private int _wearedTitleId;

    public string FriendCode { get; }
    public bool IsLocal { get; }
    public bool HasData { get { lock (_lock) return _hasData; } }
    public DateTime LoadedAt { get { lock (_lock) return _loadedAt; } }
    public bool HasRoundSnapshot { get { lock (_lock) return _hasRoundSnapshot; } }
    public int WearedTitleId { get { lock (_lock) return _wearedTitleId; } }

    public CustomAchievementData(string friendCode, bool isLocal = false)
    {
        FriendCode = friendCode;
        IsLocal = isLocal;
    }

    public IReadOnlyList<AchievementRecord> Records
    {
        get { lock (_lock) return _records.Values.Select(x => x.Clone()).ToList(); }
    }

    public IReadOnlyCollection<int> RoundStartUnlocked
    {
        get { lock (_lock) return _roundStartUnlocked.ToArray(); }
    }

    public bool HasPendingChanges
    {
        get { lock (_lock) return _pendingIds.Count > 0; }
    }

    public int UnlockedCount
    {
        get { lock (_lock) return _records.Values.Count(IsUnlocked); }
    }

    public int TotalCount => AchievementRegistry.GetAll().Count;

    public static bool IsUnlocked(AchievementRecord record)
        => record != null && (record.Unlocked || (record.Required > 0 && record.Progress >= record.Required));

    public bool Has(int id)
    {
        lock (_lock) return _records.TryGetValue(id, out var record) && IsUnlocked(record);
    }

    public int GetProgress(int id)
    {
        lock (_lock) return _records.TryGetValue(id, out var record) ? record.Progress : 0;
    }

    public AchievementRecord Get(int id)
    {
        lock (_lock) return _records.TryGetValue(id, out var record) ? record.Clone() : null;
    }

    public void SetWearedTitle(int achievementId)
    {
        lock (_lock) _wearedTitleId = Math.Max(0, achievementId);
    }

    public void MergeCloud(IEnumerable<AchievementRecord> records)
    {
        if (records == null) return;

        lock (_lock)
        {
            foreach (var incoming in records)
            {
                if (incoming == null || incoming.Id <= 0) continue;

                if (!_records.TryGetValue(incoming.Id, out var current))
                {
                    _records[incoming.Id] = incoming.Clone();
                    continue;
                }

                current.Progress = Math.Max(current.Progress, incoming.Progress);
                if (incoming.Required > 0) current.Required = incoming.Required;
                if (incoming.Unlocked) current.Unlocked = true;
                if (string.IsNullOrEmpty(current.UnlockedAt)) current.UnlockedAt = incoming.UnlockedAt ?? "";
                if (string.IsNullOrEmpty(current.Name)) current.Name = incoming.Name ?? "";
                if (string.IsNullOrEmpty(current.Description)) current.Description = incoming.Description ?? "";
            }

            _hasData = true;
            _loadedAt = DateTime.UtcNow;
        }
    }

    public bool RecordUnit(AchievementBase definition)
    {
        if (definition == null) return false;

        lock (_lock)
        {
            var record = EnsureRecord(definition);
            if (IsUnlocked(record)) return false;

            record.Required = Math.Max(1, definition.RequiredValue);
            record.Progress = definition.IsProgressAchievement
                ? Math.Min(record.Progress + 1, record.Required)
                : record.Required;
            record.Unlocked = record.Progress >= record.Required;
            if (record.Unlocked && string.IsNullOrEmpty(record.UnlockedAt))
                record.UnlockedAt = DateTime.UtcNow.ToString("o");

            _pendingIds.Add(definition.Id);
            return true;
        }
    }

    public IReadOnlyList<AchievementRecord> GetPendingRecords()
    {
        lock (_lock)
        {
            return _pendingIds
                .Where(_records.ContainsKey)
                .Select(id => _records[id].Clone())
                .ToList();
        }
    }

    public void MarkUploaded(AchievementRecord uploaded)
    {
        if (uploaded == null) return;

        lock (_lock)
        {
            if (!_records.TryGetValue(uploaded.Id, out var current)) return;
            if (current.Progress == uploaded.Progress &&
                current.Required == uploaded.Required &&
                current.Unlocked == uploaded.Unlocked)
                _pendingIds.Remove(uploaded.Id);
        }
    }

    public IReadOnlyList<int> GetNewlyUnlockedIds()
    {
        lock (_lock)
        {
            if (!_hasRoundSnapshot) return Array.Empty<int>();
            return _records.Values
                .Where(IsUnlocked)
                .Select(x => x.Id)
                .Where(id => !_roundStartUnlocked.Contains(id))
                .ToList();
        }
    }

    public void StartRound()
    {
        lock (_lock)
        {
            _roundStartUnlocked = _records.Values.Where(IsUnlocked).Select(x => x.Id).ToHashSet();
            _hasRoundSnapshot = true;
        }
    }

    public void CompleteRound()
    {
        lock (_lock)
        {
            if (_pendingIds.Count > 0) return;
            _roundStartUnlocked = _records.Values.Where(IsUnlocked).Select(x => x.Id).ToHashSet();
            _hasRoundSnapshot = true;
        }
    }

    private AchievementRecord EnsureRecord(AchievementBase definition)
    {
        if (_records.TryGetValue(definition.Id, out var record)) return record;

        record = new AchievementRecord
        {
            Id = definition.Id,
            Name = definition.Name ?? "",
            Description = definition.Description ?? "",
            Required = Math.Max(1, definition.RequiredValue),
        };
        _records[definition.Id] = record;
        return record;
    }
}
