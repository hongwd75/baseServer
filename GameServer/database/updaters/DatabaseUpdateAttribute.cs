﻿namespace Project.GS.DatabaseUpdate;

/// <summary>
/// Attribute that denotes a class as a database converter
/// from previous version to the specified in attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
public class DatabaseUpdateAttribute : Attribute
{
    //private int m_targetVersion;

    /// <summary>
    /// Constructs new attribute for database updater classes
    /// </summary>
    public DatabaseUpdateAttribute()
    {
    }
}