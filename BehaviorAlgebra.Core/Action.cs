using System;
using System.Collections.Generic;

namespace BehaviorAlgebra.Core
{
    /// <summary>
    /// Действие, которое может выполнить система.
    /// Например: "Погрузка контейнера i на автокар #1".
    /// </summary>
    public sealed class Action : IEquatable<Action>
    {
        public string Name { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }

        public Action(string name, IReadOnlyDictionary<string, object> parameters)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public bool Equals(Action? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Name != other.Name) return false;
            if (Parameters.Count != other.Parameters.Count) return false;

            foreach (var kv in Parameters)
            {
                if (!other.Parameters.TryGetValue(kv.Key, out var value)) return false;
                if (!Equals(kv.Value, value)) return false;
            }
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as Action);
        public override int GetHashCode() => HashCode.Combine(Name, Parameters.Count);
        public override string ToString() => Name;
    }
}