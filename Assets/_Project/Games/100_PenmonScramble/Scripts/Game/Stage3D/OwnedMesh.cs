using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>実行時に作ったメッシュ（シーンに保存しない作り物）を、持ち主のオブジェクトが消えるときに一緒に消す</summary>
    public class OwnedMesh : MonoBehaviour
    {
        public Mesh Mesh { get; set; }

        private void OnDestroy()
        {
            if (Mesh != null)
            {
                Destroy(Mesh);
            }
        }
    }
}
