/************************************************************************************
 Copyright: Copyright 2014 Beijing Noitom Technology Ltd. All Rights reserved.
 Pending Patents: PCT/CN2014/085659 PCT/CN2014/071006

 Licensed under the Perception Neuron SDK License Beta Version (the “License");
 You may only use the Perception Neuron SDK when in compliance with the License,
 which is provided at the time of installation or download, or which
 otherwise accompanies this software in the form of either an electronic or a hard copy.

 A copy of the License is included with this package or can be obtained at:
 http://www.neuronmocap.com

 Unless required by applicable law or agreed to in writing, the Perception Neuron SDK
 distributed under the License is provided on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing conditions and
 limitations under the License.
************************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using NeuronDataReaderManaged;
using Neuron;

namespace Neuron
{
	// cache motion data and parse to animator
	public class NeuronActor
	{
		public static int MaxFrameDataLength
		{
			//get { return ( (int)NeuronBones.NumOfBones + 1 ) * 16; }
			get { return 21 * 16 + 2; }
		}
	
		public delegate bool 							NoFrameDataDelegate();
		public delegate bool							ResumeFrameDataDelegate();
		
		static float									NeuronUnityLinearScale = 0.01f;
	
		BvhDataHeader									header;
		CalcDataHeader									CalcdHeader;
		float[]											data = new float[MaxFrameDataLength];
		List<NoFrameDataDelegate>						noFrameDataCallbacks = new List<NoFrameDataDelegate>();
		List<ResumeFrameDataDelegate>					resumeFrameDataCallbacks = new List<ResumeFrameDataDelegate>();
		
		public Guid										guid = Guid.NewGuid();
		public NeuronSource								owner = null;
		public float[]									boneSizes = new float[(int)NeuronBones.NumOfBones];
		
		public int										actorID { get; private set; }
		public DataVersion								version { get { return header.DataVersion; } }
		public string 									name { get { return header.AvatarName; } }
		public int										index { get { return (int)header.AvatarIndex; } }
		public bool										withDisplacement { get { return header.bWithDisp != 0; } }
		public bool										withReference { get { return header.bWithReference != 0; } }
		public int										dataCount { get { return (int)header.DataCount; } }
		public int										timeStamp = 0;
		
		public void RegisterNoFrameDataCallback( NoFrameDataDelegate callback )
		{
			if( callback != null )
			{
				noFrameDataCallbacks.Add( callback );
			}
		}
		
		public void UnregisterNoFrameDataCallback( NoFrameDataDelegate callback )
		{
			if( callback != null )
			{
				noFrameDataCallbacks.Remove( callback );
			}
		}
		
		public void RegisterResumeFrameDataCallback( ResumeFrameDataDelegate callback )
		{
			if( callback != null )
			{
				resumeFrameDataCallbacks.Add( callback );
			}
		}
		
		public void UnregisterResumeFrameDataCallback( ResumeFrameDataDelegate callback )
		{
			if( callback != null )
			{
				resumeFrameDataCallbacks.Remove( callback );
			}
		}
		
		public NeuronActor( NeuronSource owner, int actorID )
		{
			this.owner = owner;
			this.actorID = actorID;
			
			if( owner != null )
			{
				owner.RegisterResumeActorCallback( OnResumeFrameData );
				owner.RegisterSuspendActorCallback( OnNoFrameData );
			}
		}
		
		~NeuronActor()
		{
			if( owner != null )
			{
				owner.UnregisterResumeActorCallback( OnResumeFrameData );
				owner.UnregisterSuspendActorCallback( OnNoFrameData );
			}
		}
		
//		public void OnReceivedMotionData( BvhDataHeader header, IntPtr data )
//		{
//			this.header = header;
//			try
//			{
//				Marshal.Copy( data, this.data, 0, (int)header.DataCount );
//				timeStamp = GetTimeStamp();
//			}
//			catch( Exception e )
//			{
//				Debug.LogException( e );
//			}
//		}
		
		public void OnReceivedMotionData( CalcDataHeader header, IntPtr data )
		{
			this.CalcdHeader = header;
			try
			{
//				Debug.Log("header::" + header.DataCount + "," + header.FrameIndex + ",name "+header.AvatarName);
				//Marshal.Copy( data, this.data, 0, (int)header.DataCount );
				Marshal.Copy(data, this.data, 0, 338);
				timeStamp = GetTimeStamp();
			}
			catch( Exception e )
			{
				Debug.LogException( e );
			}
		}		
		
				
		public virtual void OnNoFrameData( NeuronActor actor )
		{
			for( int i = 0; i < noFrameDataCallbacks.Count; ++i )
			{
				noFrameDataCallbacks[i]();
			}
		}		
		
		public virtual void OnResumeFrameData( NeuronActor actor  )
		{
			for( int i = 0; i < resumeFrameDataCallbacks.Count; ++i )
			{
				resumeFrameDataCallbacks[i]();
			}
		}
		
		public float[] GetData()
		{
			return data;
		}
		
		public BvhDataHeader GetHeader()
		{
			return header;
		}
		
		public CalcDataHeader GetCalcHeader()
		{
			return CalcdHeader;
		}
		
		public static int GetTimeStamp()
		{
			return DateTime.Now.Hour * 3600 * 1000 + DateTime.Now.Minute * 60 * 1000 + DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;
		}
		
//		public Vector3 GetReceivedPosition( NeuronBones bone )
//		{
//			Debug.Log("getreceive postion:" + bone);
//			int offset = 0;
//			if( header.bWithReference != 0 )
//			{
//				// skip reference
//				offset += 6;
//			}
//			
//			// got bone position data only when displacement is open or the bone is hips
//			if( header.bWithDisp != 0 || bone == NeuronBones.Hips )
//			{
//				// Hips position + Hips rotation + 58 * ( position + rotation )
//				offset += (int)bone * 6;
//				return new Vector3( -data[offset] * NeuronUnityLinearScale, data[offset+1] * NeuronUnityLinearScale, data[offset+2] * NeuronUnityLinearScale );
//			}
//			
//			return Vector3.zero;
//		}
		
		public CalcDataBody GetCalcReceivedPosition( NeuronBones bone)
		{
			//Debug.Log("getcalcreceive postion:" + bone);
			var offset = 0;
			if( header.bWithReference != 0 )
			{
				// skip reference
				offset += 16; //6;
			}

			//Debug.Log("header:" + header.bWithDisp+", off:"+offset);
			//Debug.Log("data:" + data.Length + "," + dataCount);
			// got bone position data only when displacement is open or the bone is hips
			//if( header.bWithDisp != 0 || bone == NeuronBones.Hips )
			if( header.bWithDisp == 0 || bone == NeuronBones.Hips )
			{
				// Hips position + Hips rotation + 58 * ( position + rotation )
				offset += (int)bone * 16;
				var p = new Vector3(
					-this.data[offset],
					-this.data[offset+2],
					this.data[offset+1]
				);
//								var p = new Vector3(
//					-this.data[offset] * NeuronUnityLinearScale,
//					this.data[offset+1] * NeuronUnityLinearScale,
//					this.data[offset+2] * NeuronUnityLinearScale 
//				);

				offset += 3;
				var v = new Vector3(
					this.data[offset],
					this.data[offset+1],
					this.data[offset+2]
				);
				offset += 3;
				var a = new Vector3(
					this.data[offset],
					this.data[offset+1],
					this.data[offset+2]
				);
				offset += 3;
				var q = new Quaternion(
					this.data[offset+1],
					this.data[offset+2],
					this.data[offset+3],
					this.data[offset]
				);
				offset += 4;
				var g = new Vector3(
					this.data[offset],
					this.data[offset+1],
					this.data[offset+2]
				);
					
				// contact on ground: right->left
				offset += 3;
				var right = data[offset] * NeuronUnityLinearScale;
				var left = data[offset + 1] * NeuronUnityLinearScale;
				
				Debug.Log("foot:" + right + ","+ left);
				Debug.Log("getreceived position :"+
				          "p:" + p.x +"," + p.y +"," +p.z +"," +  
				          "v:" + v.x +"," + v.y +"," +v.z +"," +  
				          "a:" + a.x +"," + a.y +"," +a.z +"," +  
				          "q:" + q.x +"," + q.y +"," +q.z +"," + q.w +"," +  
				          "g:" + g.x +"," + g.y +"," +g.z  
				          );
				return new CalcDataBody(p,v,a,q,g);
			}

			return new CalcDataBody();
		}
		
		public Vector3 GetReceivedRotation( NeuronBones bone )
		{
			int offset = 0;
			if( header.bWithReference != 0 )
			{
				// skip reference
				offset += 6;
			}
			
			if( header.bWithDisp != 0 )
			{
				// Hips position + Hips rotation + 58 * ( position + rotation )
				offset += 3 + (int)bone * 6;
			}
			else
			{
				// Hips position + Hips rotation + 58 * rotation
				offset += 3 + (int)bone * 3;
			}
			
			return new Vector3( data[offset+1], -data[offset], -data[offset+2] );
		}
	}
}