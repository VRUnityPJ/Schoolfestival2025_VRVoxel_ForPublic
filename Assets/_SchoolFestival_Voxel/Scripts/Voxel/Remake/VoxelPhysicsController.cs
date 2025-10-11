using UnityEngine;

// RigidbodyとColliderが必須であることを示す
namespace SchoolFestival_Voxel.Scripts.Voxel.Remake
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public class VoxelPhysicsController : MonoBehaviour
    {
        [SerializeField] private ChunkManager _chunkManager;
        [SerializeField] private float _collisionPadding = 0.01f; // 衝突判定の僅かな余白
        [SerializeField] private float _groundCheckDistance = 0.05f;
        [SerializeField] private bool _enableStepUp = true; // ステップアップ機能のON/OFF
        [SerializeField] [Range(1, 5)] private int _maxStepHeight = 1; // 乗り越えられる最大の段差（ボクセル単位）
        [SerializeField] private float _stepUpPower = 0.1f;
        


        private Rigidbody _rb;
        private BoxCollider _collider;
        private bool _isFloatMode = true;

        public void InVoxelMode()
        {
            _isFloatMode = true;
            _collider.isTrigger = true;
        }

        public void OutVoxelMode()
        {
            _isFloatMode = false;
            _collider.isTrigger = false;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<BoxCollider>();
        }
        
        void FixedUpdate()
        {
            if(!_isFloatMode)return;
            bool grounded = IsGrounded();
            Vector3 velocity = _rb.linearVelocity;

            // 1. 接地状態の処理
            // もし地面にいて、かつ下向きの速度があるなら（重力で沈むのを防ぐ）
            if (grounded && velocity.y < 0)
            {
                velocity.y = 0;

                // ★★★ 以前の snapping（位置補正）処理をここに移動し、接地した瞬間だけに行う ★★★
                // これにより、不必要な毎フレームの引き上げを防ぎ、ガタつきをなくす
                Bounds bounds = _collider.bounds;
                Vector3 feetPos = bounds.center - new Vector3(0, bounds.extents.y, 0);
                Vector3Int gridPos = Vector3Int.FloorToInt(feetPos / _chunkManager.voxelSize);
                float groundY = (gridPos.y + 1) * _chunkManager.voxelSize + bounds.extents.y;
                // わずかな差であれば、ゆっくり地面に吸着させる
                transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, groundY, transform.position.z), 0.5f);
            }

            // 2. 左右・前後の移動による衝突チェック（変更なし）
            Vector3 moveDelta = velocity * Time.fixedDeltaTime;
            Bounds currentBounds = _collider.bounds;
            
            // X軸（左右）の移動
            if (moveDelta.x != 0)
            {
                Vector3 direction = moveDelta.x > 0 ? Vector3.right : Vector3.left;
                if (CheckAxisCollision(direction, currentBounds, moveDelta))
                {
                    // 衝突した場合、まずステップアップを試みる
                    if (!_enableStepUp || !TryStepUp(direction))
                    {
                        // ステップアップがOFFか、失敗した場合のみ、速度を0にする
                        velocity.x = 0;
                    }
                }
            }

            // Z軸（前後）の移動
            if (moveDelta.z != 0)
            {
                Vector3 direction = moveDelta.z > 0 ? Vector3.forward : Vector3.back;
                if (CheckAxisCollision(direction, currentBounds, moveDelta))
                {
                    if (!_enableStepUp || !TryStepUp(direction))
                    {
                        velocity.z = 0;
                    }
                }
            }
        
            // Y軸（上下）の移動
            if (moveDelta.y > 0 && CheckAxisCollision(Vector3.up, currentBounds, moveDelta)) velocity.y = 0;

            _rb.linearVelocity = velocity;
            
        }

        /// <summary>
        /// オブジェクトが地面に接地しているかを判定する
        /// </summary>
        private bool IsGrounded()
        {
            Bounds bounds = _collider.bounds;
            int count = 0;
            // 足元の4つの角の少し下をチェックする
            Vector3 ext = bounds.extents;
            Vector3[] corners = {
                new Vector3(ext.x, -ext.y - _groundCheckDistance, ext.z),
                new Vector3(-ext.x, -ext.y - _groundCheckDistance, ext.z),
                new Vector3(ext.x, -ext.y - _groundCheckDistance, -ext.z),
                new Vector3(-ext.x, -ext.y - _groundCheckDistance, -ext.z)
            };

            foreach (var corner in corners)
            {
                
                Vector3 checkPos = transform.position + corner;
                Vector3Int gridPos = Vector3Int.FloorToInt(checkPos / _chunkManager.voxelSize);
                if (_chunkManager.IsSolid(gridPos))
                {
                    count++;
                    if(count >= 3)
                        return true; // いずれかの角が地面にめり込んでいれば接地している
                }
            }
            return false;
        }
        
        /// <summary>
        /// 指定された方向にステップアップを試みる
        /// </summary>
        /// <param name="direction">移動方向</param>
        /// <returns>ステップアップに成功したらtrue</returns>
        private bool TryStepUp(Vector3 direction)
        {
            Bounds bounds = _collider.bounds;
            
            // キャラクターの足元で、進行方向の最も低い衝突点をチェックする
            Vector3 checkStartPoint = bounds.center - new Vector3(0, bounds.extents.y - 0.1f, 0);
            
            // 1段から_maxStepHeight段まで、乗り越えられるかチェック
            for (int step = 1; step <= _maxStepHeight; step++)
            {
                // 1. 障害物の高さチェック
                // 進行方向の `step` 段目の高さのボクセルが空いているか？
                Vector3 stepCheckPos = checkStartPoint + direction * bounds.extents.x + new Vector3(0, step * _chunkManager.voxelSize, 0);
                Vector3Int stepGridPos = Vector3Int.FloorToInt(stepCheckPos / _chunkManager.voxelSize);

                if (!_chunkManager.IsSolid(stepGridPos))
                {
                    // 2. 頭上の空間チェック
                    // `step` 段目の高さから、キャラクターの身長分離れた場所まで、すべて空いているか？
                    bool headRoomClear = true;
                    float characterHeight = _collider.size.y;
                    for (float height = step; height < characterHeight + step; height += 0.5f) // 0.5fごとなど、細かくチェック
                    {
                        Vector3 headCheckPos = checkStartPoint + direction * bounds.extents.x + new Vector3(0, height * _chunkManager.voxelSize, 0);
                        Vector3Int headGridPos = Vector3Int.FloorToInt(headCheckPos / _chunkManager.voxelSize);
                        if (_chunkManager.IsSolid(headGridPos))
                        {
                            headRoomClear = false;
                            break; // 障害物が見つかったのでチェック終了
                        }
                    }

                    if (headRoomClear)
                    {
                        // 3. 実行
                        // すべてのチェックをクリアしたので、キャラクターを上に移動させる
                        // transform.position += new Vector3(0, step * _chunkManager.voxelSize, 0);
                        _rb.AddForce(Vector3.up * (step * _chunkManager.voxelSize*_stepUpPower), ForceMode.Impulse);
                        // float requiredVelY = (step * _chunkManager.voxelSize*_stepUpPower) / Time.fixedDeltaTime;
                        // var vector3 = _rb.linearVelocity;
                        // vector3.y = requiredVelY;
                        // _rb.linearVelocity = vector3;
                        return true; // ステップアップ成功
                    }
                }
            }
            
            return false; // どの高さのステップも乗り越えられなかった
        }
        

        // 以前のCheckAxisCollisionメソッド（変更なし）
        private bool CheckAxisCollision(Vector3 direction, Bounds bounds, Vector3 moveDelta)
        {
            float x_norm = direction.x != 0 ? direction.x : 1;
            float y_norm = direction.y != 0 ? direction.y : 1;
            float z_norm = direction.z != 0 ? direction.z : 1;

            Vector3[] corners = new Vector3[4];
            Vector3 ext = bounds.extents;

            if (direction.y != 0)
            {
                corners[0] = new Vector3(ext.x, ext.y * y_norm, ext.z);
                corners[1] = new Vector3(-ext.x, ext.y * y_norm, ext.z);
                corners[2] = new Vector3(ext.x, ext.y * y_norm, -ext.z);
                corners[3] = new Vector3(-ext.x, ext.y * y_norm, -ext.z);
            }
            else if (direction.x != 0)
            {
                corners[0] = new Vector3(ext.x * x_norm, ext.y, ext.z);
                corners[1] = new Vector3(ext.x * x_norm, -ext.y, ext.z);
                corners[2] = new Vector3(ext.x * x_norm, ext.y, -ext.z);
                corners[3] = new Vector3(ext.x * x_norm, -ext.y, -ext.z);
            }
            else
            {
                corners[0] = new Vector3(ext.x, ext.y, ext.z * z_norm);
                corners[1] = new Vector3(-ext.x, ext.y, ext.z * z_norm);
                corners[2] = new Vector3(ext.x, -ext.y, ext.z * z_norm);
                corners[3] = new Vector3(-ext.x, -ext.y, ext.z * z_norm);
            }

            foreach (var corner in corners)
            {
                Vector3 checkPos = transform.position + corner + moveDelta + direction * _collisionPadding;
                Vector3Int gridPos = Vector3Int.FloorToInt(checkPos / _chunkManager.voxelSize);
                if (_chunkManager.IsSolid(gridPos))
                {
                    return true;
                }
            }
            return false;
        }
    
    }
}