﻿using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace TpacTool.Lib
{
	public class Metamesh : AssetItem
	{
		public static readonly Guid TYPE_GUID = Guid.Parse("a08f8b97-197c-4bea-b95b-53846cae834e");

		/*
		 * Version 1: Since 1.4.3. Added tangent space transform quaternion (a.k.a Q-Tangent) for the vertex stream data.
		 */
		public static readonly uint LATEST_VERSION = 1;

		public Guid Material { set; get; } // not sure. but billboard texture will ref the same guid

		// maybe max rendering distance. float.max for most "big" models. 20~60 for many lod-less small things
		public float UnknownFloat { set; get; }

		[NotNull]
		public string UnknownString { set; get; }
		/* Used by armors, and empty for others. These values are used:
		   human_body
		   head
		   helmet_head_shoulder
		   detailed_head
		   head&shoulders
		   cape_body
		   empire_helmet_g
		   arms
		   legs
		 */

		public Guid ClothMetamesh { set; get; }

		public uint UnknownUint { set; get; } // 0 for all metameshes

		public uint ClothUint { set; get; }

		[NotNull]
		public string ClothString { set; get; }

		public Guid Original { set; get; }

		public List<Guid> Variations { private set; get; }

		public bool UnknownBool1 { set; get; } // true for all metameshes

		public bool UnknownBool2 { set; get; } // true for all metameshes

		public List<Mesh> Meshes { private set; get; }

		[CanBeNull]
		public ExternalLoader<EditmodeMiscData> EditmodeMisc { set; get; }

		public Metamesh() : base(TYPE_GUID)
		{
			this.Material = Guid.Empty;
			this.ClothString = String.Empty;
			this.Variations = new List<Guid>();
			this.Meshes = new List<Mesh>();

			this.Version = LATEST_VERSION;
		}

		public override void ReadMetadata(BinaryReader stream, int totalSize)
		{
			var version = stream.ReadUInt32();
			Material = stream.ReadGuid();
			UnknownFloat = stream.ReadSingle();
			UnknownString = stream.ReadSizedString();
			ClothMetamesh = stream.ReadGuid();
			if (version >= 1)
			{
				UnknownUint = stream.ReadUInt32();
				ClothUint = stream.ReadUInt32();
				if (ClothUint > 0)
				{
					ClothString = stream.ReadSizedString();
				}
			}

			int meshCount = stream.ReadInt32();
			for (int i = 0; i < meshCount; i++)
			{
				var mesh = new Mesh(stream);
				Meshes.Add(mesh);
			}

			Original = stream.ReadGuid();
			int guidCount = stream.ReadInt32();
			Variations.Clear();
			Variations.Capacity = guidCount;
			for (int i = 0; i < guidCount; i++)
			{
				Variations.Add(stream.ReadGuid());
			}

			UnknownBool1 = stream.ReadBoolean();
			UnknownBool2 = stream.ReadBoolean();
		}

		public override void ConsumeDataSegments(AbstractExternalLoader[] externalData)
		{
			foreach (var externalLoader in externalData)
			{
				var ownerGuid = externalLoader.OwnerGuid;
				foreach (var mesh in Meshes)
				{
					if (ownerGuid != mesh.Guid)
						continue;
					var editData = externalLoader as ExternalLoader<MeshEditData>;
					if (editData != null)
					{
						mesh.EditData = editData;
						continue;
					}
					var vertexStream = externalLoader as ExternalLoader<VertexStreamData>;
					if (vertexStream != null)
					{
						mesh.VertexStream = vertexStream;
					}
				}

				var editmodeData = externalLoader as ExternalLoader<EditmodeMiscData>;
				if (editmodeData != null)
				{
					EditmodeMisc = editmodeData;
				}
			}

			foreach (var mesh in Meshes)
			{
				if (mesh.VertexStream == null)
					continue;

				if (Version >= 1)
				{
					mesh.VertexStream.UserData[VertexStreamData.KEY_HAS_QTANGENT] = true;
				}

				var vertCount = mesh.VertexCount;
				if (vertCount >= UInt16.MaxValue)
				{
					mesh.VertexStream.UserData[VertexStreamData.KEY_IS_32BIT_INDEX] = true;
				}
			}
			base.ConsumeDataSegments(externalData);
		}
	}
}