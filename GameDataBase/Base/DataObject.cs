﻿/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.ComponentModel;
using Project.Database.Attributes;
using Project.Database.UniqueID;

namespace Project.Database
{
	/// <summary>
	/// Abstract Baseclass for all DataObject's. All Classes that are derived from this class
	/// are stored in a Datastore
	/// </summary>
	public abstract class DataObject : ICloneable
	{
		bool m_allowAdd = true;
		bool m_allowDelete = true;

		/// <summary>
		/// Default-Construktor that generates a new Object-ID and set
		/// Dirty and Persisted to <c>false</c>
		/// </summary>
		protected DataObject()
		{
			ObjectId = IDGenerator.GenerateID();
			IsPersisted = false;
			AllowAdd = true;
			AllowDelete = true;
			IsDeleted = false;
		}

		/// <summary>
		/// The table name which own he object 
		/// </summary>
		[Browsable(false)]
		public virtual string TableName
		{
			get
			{
				return AttributesUtils.GetTableName(GetType());
			}
		}

		/// <summary>
		/// Load object in cache or not?
		/// </summary>
		[Browsable(false)]
		public virtual bool UsesPreCaching
		{
			get
			{
				return AttributesUtils.GetPreCachedFlag(GetType());
			}
		}

		/// <summary>
		/// Is this object also in the database?
		/// </summary>
		[Browsable(false)]
		public bool IsPersisted { get; set; }

		/// <summary>
		/// Can this object added to the DB?
		/// </summary>
		[Browsable(false)]
		public virtual bool AllowAdd 
		{
			get { return m_allowAdd; }
			set { m_allowAdd = value; }
		}

		/// <summary>
		/// Can this object be deleted from the DB?
		/// </summary>
		[Browsable(false)]
		public virtual bool AllowDelete
		{
			get { return m_allowDelete; }
			set { m_allowDelete = value; }
		}

		/// <summary>
		/// Index of the object in his table
		/// </summary>
		[Browsable(false)]
		public string ObjectId { get; set; }

		/// <summary>
		/// Is object different than object in the DB?
		/// </summary>
		[Browsable(false)]
		public virtual bool Dirty { get; set; }

		/// <summary>
		/// Has this object been deleted from the database
		/// </summary>
		[Browsable(false)]
		public virtual bool IsDeleted { get; set; }

		/// <summary>
		/// Default field added to all DataObject.
		/// Last time this record was updated.
		/// Return UTC Now to update table's "LastTimeRowUpdated"
		/// for Maintenance purpose.
		/// </summary>
		[DataElement(AllowDbNull = false, Index = false)]
		public DateTime LastTimeRowUpdated 
		{
			get { return DateTime.UtcNow; }
			set { Dirty = true; }
		}

		#region ICloneable Member

		/// <summary>
		/// Clone the current object and return the copy
		/// </summary>
		/// <returns></returns>
		public object Clone()
		{
			var obj = (DataObject) MemberwiseClone();
			obj.ObjectId = IDGenerator.GenerateID();
			return obj;
		}

		#endregion

		public override string ToString()
		{
			return string.Format("DataObject: {0}, ObjectId{{{1}}}", TableName, ObjectId);
		}
	}
}