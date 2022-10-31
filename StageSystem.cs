using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StageSystem : MonoBehaviour
{
    //タイルの種類
    private enum TileType
    {
        NONE,//何もない空間
        GROUND,//地面
        TARGET,//目的地
        PLAYER,//プレイヤー
        BLOCK,//ブロック
        PLAYER_ON_TARGET,//プレイヤー（目的地の上）
        BLOCK_ON_TARGET,//ブロック（目的地の上）
    }

    //方向の種類
    private enum DirectionType
    {
        UP,//上
        DOWN,//下
        RIGHT,//右
        LEFT,//左
    }
    #region 変数
        #region テキストファイルからの変換用
        public TextAsset stageFile;//ステージ構造が記述されたテキストファイル
        private int _rows;//行
        private int _columns;//列
        private TileType[,] _tileList;//タイル情報の管理用二次元配列
        #endregion

        #region マップ用変数
        public float _tileSize = default;//タイルのサイズ
        [SerializeField]
        private Sprite _groundSprite;//地面のスプライト
        [SerializeField]
        private Sprite _targetSprite;//目的地のスプライト
        [SerializeField]
        private Sprite _playerSprite;//プレイヤーのスプライト
        [SerializeField]
        private Sprite _blockSprite;//ブロックのスプライト
        private GameObject _player;//プレイヤーのゲームオブジェクト
        private Vector2 _middleOffset;//中心位置
        private int _blockCount = 0;//ブロックの数
        private Dictionary<GameObject, Vector2Int> gameObjectPosTable = new Dictionary<GameObject, Vector2Int>();//各一に存在するゲームオブジェクトを管理する配列
    #endregion

        #region プレイヤー用変数
        private bool _IsClear = false;//ゲームをクリアした場合true
        private int _sceneIndex;
    #endregion
        #region UI用変数
        public Text _stepCountText;//歩数(数字)のテキスト
        private int _stepCount = 0;//歩数のカウント
        private int _minStepCount = 0;
        public Text _stepText;//歩数のテキスト
        private float duration = 2.0F;//色変更の間隔
        [SerializeField]
        private Button _retryButton;//リザルトのボタン
        [SerializeField]
        private Button _nextButton;//ネクストのボタン
        [SerializeField]
        private GameObject _rezultUi;//リザルトのゲームオブジェクト
        [SerializeField]
        private Text _minStepsText;
        [SerializeField]
        private Image _crownImage;
    #endregion

    #endregion

    private void Start()
    {
        _rezultUi.gameObject.SetActive(false);
        //タイルの情報を読み込む
        LoadTileData();
        //ステージを作成
        CreateStage();
    }

    //プレイヤー移動処理
    private void Update()
    {
        if (_IsClear)
        {
            return;
        }

        // 上矢印が押された場合
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            // プレイヤーが上に移動できるか検証
            TryMovePlayer(DirectionType.UP);
        }

        // 右矢印が押された場合
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            // プレイヤーが右に移動できるか検証
            TryMovePlayer(DirectionType.RIGHT);
        }

        // 下矢印が押された場合
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            // プレイヤーが下に移動できるか検証
            TryMovePlayer(DirectionType.DOWN);
        }

        // 左矢印が押された場合
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            // プレイヤーが左に移動できるか検証
            TryMovePlayer(DirectionType.LEFT);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _sceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(_sceneIndex);
        }

        //durationの時間ごとに色が変わる
        float phi = Time.time / duration * 2 * Mathf.PI;
        float amplitude = Mathf.Cos(phi) * 0.5F + 0.5F;
        //色をRGBではなくHSVで指定
        _stepText.color = Color.HSVToRGB(amplitude, 1, 1);

    }


    //-------------------------------------------------------------------------------------
    //この下タイル情報の書き込み＆読み込み

    //タイルの情報を読み込む
    private void LoadTileData()
    {
        //タイルの情報を一行ごとに分割
        var lines = stageFile.text.Split(new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries);

        //タイルの列数を計算
        var nums = lines[0].Split(new[] { ',' });

        //タイルの列数と行数を保持
        _rows = lines.Length;
        _columns = nums.Length;

        //タイル情報をint型の二次元配列で保持
        _tileList = new TileType[_columns, _rows];

        for (int y = 0; y < _rows; y++)
        {
            //一文字ずつ取得
            var st = lines[y];
            nums = st.Split(new[] { ',' });
            for(int x = 0; x < _columns; x++)
            {
                //読み込んだ文字を数値に変換して保持
                _tileList[x, y] = (TileType)int.Parse(nums[x]);
            }
        }
    }

    private void CreateStage()
    {
        //ステージの中心位置を計算
        _middleOffset.x = _columns * _tileSize * 0.5f - _tileSize * 0.5f;
        _middleOffset.y = _rows * _tileSize * 0.5f - _tileSize * 0.5f;

        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _columns; x++)
            {
                var val = _tileList[x, y];
                //何もない場所は無視
                if (val == TileType.NONE)
                {
                    continue;
                }
                //タイルの名前に行番号と列番号を付与
                var name = "tile" + y + "_" + x;

                //タイルのゲームオブジェクトを作成
                var tile = new GameObject(name);

                //タイルにスプライトを描画する
                var sr = tile.AddComponent<SpriteRenderer>();

                //タイルのスプライトを設定
                sr.sprite = _groundSprite;

                //タイルの位置を設定
                tile.transform.position = GetDisplayPosition(x, y);

                // 目的地の場合
                if (val == TileType.TARGET)
                {
                    // 目的地のゲームオブジェクトを作成
                    var destination = new GameObject("destination");

                    // 目的地にスプライトを描画する機能を追加
                    sr = destination.AddComponent<SpriteRenderer>();

                    // 目的地のスプライトを設定
                    sr.sprite = _targetSprite;

                    // 目的地の描画順を手前にする
                    sr.sortingOrder = 1;

                    // 目的地の位置を設定
                    destination.transform.position = GetDisplayPosition(x, y);
                }

                //プレイヤーの場合
                if (val == TileType.PLAYER)
                {
                    // プレイヤーのゲームオブジェクトを作成
                    _player = new GameObject("player");

                    // プレイヤーにスプライトを描画する機能を追加
                    sr = _player.AddComponent<SpriteRenderer>();

                    // プレイヤーのスプライトを設定
                    sr.sprite = _playerSprite;

                    // プレイヤーの描画順を手前にする
                    sr.sortingOrder = 2;

                    // プレイヤーの位置を設定
                    _player.transform.position = GetDisplayPosition(x, y);

                    // プレイヤーを連想配列に追加
                    gameObjectPosTable.Add(_player, new Vector2Int(x, y));
                }

                // ブロックの場合
                else if (val == TileType.BLOCK)
                {
                    // ブロックの数を増やす
                    _blockCount++;

                    // ブロックのゲームオブジェクトを作成
                    var block = new GameObject("block" + _blockCount);

                   // ブロックにスプライトを描画する機能を追加
                    sr = block.AddComponent<SpriteRenderer>();

                    // ブロックのスプライトを設定
                    sr.sprite = _blockSprite;

                    // ブロックの描画順を手前にする
                    sr.sortingOrder = 2;

                    // ブロックの位置を設定
                    block.transform.position = GetDisplayPosition(x, y);

                    // ブロックを連想配列に追加
                    gameObjectPosTable.Add(block, new Vector2Int(x, y));
                }
            }
        }
    }

    //指定された行番号と列番号からスプライトの表示位置を計算して返す
    private Vector2 GetDisplayPosition(int x, int y)
    {
        return new Vector2(x * _tileSize - _middleOffset.x,
            y * -_tileSize + _middleOffset.y);
    }

    //-------------------------------------------------------------------------------------
    //この下倉庫番のロジック
    #region 倉庫番ロジック（チェック関数）
    //指定された位置に存在するゲームオブジェクトを返す
    private GameObject GetGameObjectAtPosition(Vector2Int pos)
    {
        foreach (var pair in gameObjectPosTable)
        {
            // 指定された位置が見つかった場合
            if (pair.Value == pos)
            {
                // その位置に存在するゲームオブジェクトを返す
                return pair.Key;
            }
        }
        return null;
    }

    //指定された位置ステージ内ならtrueを返す
    private bool IsValidPosition(Vector2Int pos)
    {
        if (0 <= pos.x && pos.x < _columns && 0 <= pos.y && pos.y < _rows)
        {
            return _tileList[pos.x, pos.y] != TileType.NONE;
        }
        return false;
    }

    //指定された位置のタイルがブロックならtrueを返す
    private bool IsBlock(Vector2Int pos)
    {
        var cell = _tileList[pos.x, pos.y];
        return cell == TileType.BLOCK || cell == TileType.BLOCK_ON_TARGET;
    }

    #endregion

    //-------------------------------------------------------------------------------------
    //この下移動処理
    //指定された方向にプレイヤーが移動できるか検証
    //移動できる場合は移動する
    private void TryMovePlayer(DirectionType direction)
    {
        // プレイヤーの現在地を取得
        var currentPlayerPos = gameObjectPosTable[_player];

        // プレイヤーの移動先の位置を計算
        var nextPlayerPos = GetNextPositionAlong(currentPlayerPos, direction);

        // プレイヤーの移動先がステージ内ではない場合は無視
        if (!IsValidPosition(nextPlayerPos)) return;

        // プレイヤーの移動先にブロックが存在する場合
        if (IsBlock(nextPlayerPos))
        {
            // ブロックの移動先の位置を計算
            var nextBlockPos = GetNextPositionAlong(nextPlayerPos, direction);

            // ブロックの移動先がステージ内の場合かつ
            // ブロックの移動先にブロックが存在しない場合
            if (IsValidPosition(nextBlockPos) && !IsBlock(nextBlockPos))
            {
                // 移動するブロックを取得
                var block = GetGameObjectAtPosition(nextPlayerPos);

                // プレイヤーの移動先のタイルの情報を更新
                UpdateGameObjectPosition(nextPlayerPos);

                // ブロックを移動
                block.transform.position = GetDisplayPosition(nextBlockPos.x, nextBlockPos.y);

                // ブロックの位置を更新
                gameObjectPosTable[block] = nextBlockPos;

                // ブロックの移動先の番号を更新
                if (_tileList[nextBlockPos.x, nextBlockPos.y] == TileType.GROUND)
                {
                    // 移動先が地面ならブロックの番号に更新
                    _tileList[nextBlockPos.x, nextBlockPos.y] = TileType.BLOCK;
                }
                else if (_tileList[nextBlockPos.x, nextBlockPos.y] == TileType.TARGET)
                {
                    // 移動先が目的地ならブロック（目的地の上）の番号に更新
                    _tileList[nextBlockPos.x, nextBlockPos.y] = TileType.BLOCK_ON_TARGET;
                }

                // プレイヤーの現在地のタイルの情報を更新
                UpdateGameObjectPosition(currentPlayerPos);

                // プレイヤーを移動
                _player.transform.position = GetDisplayPosition(nextPlayerPos.x, nextPlayerPos.y);

                //歩数カウント増加
                _stepCount++;
                //文字型に変換
                _stepCountText.text = _stepCount.ToString();

                // プレイヤーの位置を更新
                gameObjectPosTable[_player] = nextPlayerPos;

                // プレイヤーの移動先の番号を更新
                if (_tileList[nextPlayerPos.x, nextPlayerPos.y] == TileType.GROUND)
                {
                    // 移動先が地面ならプレイヤーの番号に更新
                    _tileList[nextPlayerPos.x, nextPlayerPos.y] = TileType.PLAYER;
                }
                else if (_tileList[nextPlayerPos.x, nextPlayerPos.y] == TileType.TARGET)
                {
                    // 移動先が目的地ならプレイヤー（目的地の上）の番号に更新
                    _tileList[nextPlayerPos.x, nextPlayerPos.y] = TileType.PLAYER_ON_TARGET;
                }
            }
        }
        // プレイヤーの移動先にブロックが存在しない場合
        else
        {
            // プレイヤーの現在地のタイルの情報を更新
            UpdateGameObjectPosition(currentPlayerPos);

            // プレイヤーを移動
            _player.transform.position = GetDisplayPosition(nextPlayerPos.x, nextPlayerPos.y);

            // プレイヤーの位置を更新
            gameObjectPosTable[_player] = nextPlayerPos;

            // プレイヤーの移動先の番号を更新
            if (_tileList[nextPlayerPos.x, nextPlayerPos.y] == TileType.GROUND)
            {
                // 移動先が地面ならプレイヤーの番号に更新
                _tileList[nextPlayerPos.x, nextPlayerPos.y] = TileType.PLAYER;
            }
            else if (_tileList[nextPlayerPos.x, nextPlayerPos.y] == TileType.TARGET)
            {
                // 移動先が目的地ならプレイヤー（目的地の上）の番号に更新
                _tileList[nextPlayerPos.x, nextPlayerPos.y] = TileType.PLAYER_ON_TARGET;
            }
            //歩数カウント増加
            _stepCount++;
            //文字型に変換
            _stepCountText.text = _stepCount.ToString();
        }
        //ゲームをクリアしたかどうか確認
        CheckCompletion();
    }

    //指定された方向の位置を返す
    private Vector2Int GetNextPositionAlong(Vector2Int pos, DirectionType direction)
    {
        switch (direction)
        {
            case DirectionType.UP:
                pos.y -= 1;
                break;

            case DirectionType.DOWN:
                pos.y += 1;
                break;

            case DirectionType.RIGHT:
                pos.x += 1;
                break;

            case DirectionType.LEFT:
                pos.x -= 1;
                break;
        }
        return pos;
    }
    private void UpdateGameObjectPosition(Vector2Int pos)
    {
        // 指定された位置のタイルの番号を取得
        var cell = _tileList[pos.x, pos.y];

        // プレイヤーもしくはブロックの場合
        if (cell == TileType.PLAYER || cell == TileType.BLOCK)
        {
            // 地面に変更
            _tileList[pos.x, pos.y] = TileType.GROUND;
        }
        // 目的地に乗っているプレイヤーもしくはブロックの場合
        else if (cell == TileType.PLAYER_ON_TARGET || cell == TileType.BLOCK_ON_TARGET)
        {
            // 目的地に変更
            _tileList[pos.x, pos.y] = TileType.TARGET;
        }
    }

    //ゲームをクリアしたかどうか確認
    private void CheckCompletion()
    {
        // 目的地に乗っているブロックの数を計算
        int blockOnTargetCount = 0;

        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _columns; x++)
            {
                if (_tileList[x, y] == TileType.BLOCK_ON_TARGET)
                {
                    blockOnTargetCount++;
                }
            }
        }
        // すべてのブロックが目的地の上に乗っている場合
        if (blockOnTargetCount == _blockCount)
        {
            // ゲームクリア
            _IsClear = true;
            //ゲームクリア表示
            _rezultUi.gameObject.SetActive(true);
            _minStepCount = int.Parse(_minStepCount.ToString());
            int difference = 0;
            difference = _stepCount - _minStepCount;
            Debug.Log(difference);
            //王冠を金色にする
            if (_minStepCount <= difference)
            {
                _crownImage.color = new Color(255, 215, 0, 255);
                //王冠を銀色にする
                if (_minStepCount +7 <= difference)
                {
                    _crownImage.color = new Color(192, 192, 192, 255);
                    //王冠を茶色にする
                    if (_minStepCount + 12 <= difference)
                    {
                        _crownImage.color = new Color(115, 78, 48, 255);
                    }
                }
            }
        }
    }

    public void OnRetry()
    {
        _sceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(_sceneIndex);
    }

    public void OnNext()
    {
        _sceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(_sceneIndex + 1);
    }

    public void OnEnd()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;//ゲームプレイ終了
#else
    Application.Quit();//ゲームプレイ終了
#endif
    }
}
