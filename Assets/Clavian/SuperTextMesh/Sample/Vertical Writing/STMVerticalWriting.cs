using UnityEngine;
using System.Collections;

public class STMVerticalWriting : MonoBehaviour {
	public Vector3 eulerRotation;
	public void RotateLetters(Vector3[] verts, Vector3[] middles, Vector3[] positions){
		for(int i=0, iL=middles.Length; i<iL; i++){
			verts[4*i+0] = RotateVertAroundMiddle(verts[4*i+0], middles[i], eulerRotation);
			verts[4*i+1] = RotateVertAroundMiddle(verts[4*i+1], middles[i], eulerRotation);
			verts[4*i+2] = RotateVertAroundMiddle(verts[4*i+2], middles[i], eulerRotation);
			verts[4*i+3] = RotateVertAroundMiddle(verts[4*i+3], middles[i], eulerRotation);
		}
	}
	public Vector3 RotateVertAroundMiddle(Vector3 vert, Vector3 middle, Vector3 euler) {
		return Quaternion.Euler(euler) * (vert - middle) + middle;
	}
}
