using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StageSystem : MonoBehaviour
{
    //�^�C���̎��
    private enum TileType
    {
        NONE,//�����Ȃ����
        GROUND,//�n��
        TARGET,//�ړI�n
        PLAYER,//�v���C���[
        BLOCK,//�u���b�N
        PLAYER_ON_TARGET,//�v���C���[�i�ړI�n�̏�j
        BLOCK_ON_TARGET,//�u���b�N�i�ړI�n�̏�j
    }

    //�����̎��
    private enum DirectionType
    {
        UP,//��
        DOWN,//��
        RIGHT,//�E
        LEFT,//��
    }
    #region �ϐ�
        #region �e�L�X�g�t�@�C������̕ϊ��p
        public TextAsset _stageFile;//�X�e�[�W�\�����L�q���ꂽ�e�L�X�g�t�@�C��
        private int _rows;//�s
        private int _columns;//��
        private TileType[,] _tileList;//�^�C�����̊Ǘ��p�񎟌��z��
        #endregion

        #region �}�b�v�p�ϐ�
        public float _tileSize = default;//�^�C���̃T�C�Y
        [SerializeField]
        private Sprite _groundSprite;//�n�ʂ̃X�v���C�g
        [SerializeField]
        private Sprite _targetSprite;//�ړI�n�̃X�v���C�g
        [SerializeField]
        private Sprite _playerSprite;//�v���C���[�̃X�v���C�g
        [SerializeField]
        private Sprite _blockSprite;//�u���b�N�̃X�v���C�g
        private GameObject _player;//�v���C���[�̃Q�[���I�u�W�F�N�g
        private Vector2 _middleOffset;//���S�ʒu
        private int _blockCount = 0;//�u���b�N�̐�
        private Dictionary<GameObject, Vector2Int> _gameObjectPosTable = new Dictionary<GameObject, Vector2Int>();//�e��ɑ��݂���Q�[���I�u�W�F�N�g���Ǘ�����z��
    #endregion

        #region �v���C���[�p�ϐ�
        private bool _isClear = false;//�Q�[�����N���A�����ꍇtrue
        private int _sceneIndex;
    #endregion

        #region UI�p�ϐ�
        public Text _stepCountText;//����(����)�̃e�L�X�g
        private int _stepCount = 0;//�����̃J�E���g
        public Text _stepText;//�����̃e�L�X�g
        private float _duration = 2.0F;//�F�ύX�̊Ԋu
        [SerializeField]
        private Button _retryButton;//���U���g�̃{�^��
        [SerializeField]
        private Button _nextButton;//�l�N�X�g�̃{�^��
        [SerializeField]
        private GameObject _rezultUi;//���U���g�̃Q�[���I�u�W�F�N�g
    #endregion

    #endregion

    private void Start()
    {
        _rezultUi.gameObject.SetActive(false);
        //�^�C���̏���ǂݍ���
        LoadTileData();
        //�X�e�[�W���쐬
        CreateStage();
    }

    //�v���C���[�ړ�����
    private void Update()
    {
        if (_isClear)
        {
            return;
        }

        // ���󂪉����ꂽ�ꍇ
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            // �v���C���[����Ɉړ��ł��邩����
            TryMovePlayer(DirectionType.UP);
        }

        // �E��󂪉����ꂽ�ꍇ
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            // �v���C���[���E�Ɉړ��ł��邩����
            TryMovePlayer(DirectionType.RIGHT);
        }

        // ����󂪉����ꂽ�ꍇ
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            // �v���C���[�����Ɉړ��ł��邩����
            TryMovePlayer(DirectionType.DOWN);
        }

        // ����󂪉����ꂽ�ꍇ
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            // �v���C���[�����Ɉړ��ł��邩����
            TryMovePlayer(DirectionType.LEFT);
        }

        //�X�y�[�X�������ꂽ�ꍇ���X�^�[�g
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //Build In Index�̒l���擾���A�i�[����
            _sceneIndex = SceneManager.GetActiveScene().buildIndex;
            //�i�[�����l�̃V�[�������[�h����
            SceneManager.LoadScene(_sceneIndex);
        }

        _sceneIndex = SceneManager.GetActiveScene().buildIndex;
        //���X�e�[�W
        if(_sceneIndex == 0)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SceneManager.LoadScene(_sceneIndex + 6);
            }
        }

        //duration�̎��Ԃ��ƂɐF���ς��
        float phi = Time.time / _duration * 2 * Mathf.PI;
        float amplitude = (Mathf.Cos(phi) * 0.5F) + 0.5F;
        //�F��RGB�ł͂Ȃ�HSV�Ŏw��
        _stepText.color = Color.HSVToRGB(amplitude, 1, 1);

    }


    //-------------------------------------------------------------------------------------
    //���̉��^�C�����̏������݁��ǂݍ���

    //�^�C���̏���ǂݍ���
    private void LoadTileData()
    {
        //�^�C���̏�����s���Ƃɕ���
        string[] lines = _stageFile.text.Split(new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries);

        //�^�C���̗񐔂��v�Z
        string[] nums = lines[0].Split(new[] { ',' });

        //�^�C���̗񐔂ƍs����ێ�
        _rows = lines.Length;
        _columns = nums.Length;

        //�^�C������int�^�̓񎟌��z��ŕێ�
        _tileList = new TileType[_columns, _rows];

        for (int y = 0; y < _rows; y++)
        {
            //�ꕶ�����擾
            string st = lines[y];
            nums = st.Split(new[] { ',' });
            for(int x = 0; x < _columns; x++)
            {
                //�ǂݍ��񂾕����𐔒l�ɕϊ����ĕێ�
                _tileList[x, y] = (TileType)int.Parse(nums[x]);
            }
        }
    }

    private void CreateStage()
    {
        //�X�e�[�W�̒��S�ʒu���v�Z
        _middleOffset.x = (_columns * _tileSize * 0.5f) - (_tileSize * 0.5f);
        _middleOffset.y = (_rows * _tileSize * 0.5f) - (_tileSize * 0.5f);

        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _columns; x++)
            {
                TileType val = _tileList[x, y];
                //�����Ȃ��ꏊ�͖���
                if (val == TileType.NONE)
                {
                    continue;
                }
                //�^�C���̖��O�ɍs�ԍ��Ɨ�ԍ���t�^
                string name = "tile" + y + "_" + x;

                //�^�C���̃Q�[���I�u�W�F�N�g���쐬
                GameObject tile = new GameObject(name);

                //�^�C���ɃX�v���C�g��`�悷��
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();

                //�^�C���̃X�v���C�g��ݒ�
                sr.sprite = _groundSprite;

                //�^�C���̈ʒu��ݒ�
                tile.transform.position = GetDisplayPosition(x, y);

                // �ړI�n�̏ꍇ
                if (val == TileType.TARGET)
                {
                    // �ړI�n�̃Q�[���I�u�W�F�N�g���쐬
                    GameObject destination = new GameObject("destination");

                    // �ړI�n�ɃX�v���C�g��`�悷��@�\��ǉ�
                    sr = destination.AddComponent<SpriteRenderer>();

                    // �ړI�n�̃X�v���C�g��ݒ�
                    sr.sprite = _targetSprite;

                    // �ړI�n�̕`�揇����O�ɂ���
                    sr.sortingOrder = 1;

                    // �ړI�n�̈ʒu��ݒ�
                    destination.transform.position = GetDisplayPosition(x, y);
                }

                //�v���C���[�̏ꍇ
                if (val == TileType.PLAYER)
                {
                    // �v���C���[�̃Q�[���I�u�W�F�N�g���쐬
                    _player = new GameObject("player");

                    // �v���C���[�ɃX�v���C�g��`�悷��@�\��ǉ�
                    sr = _player.AddComponent<SpriteRenderer>();

                    // �v���C���[�̃X�v���C�g��ݒ�
                    sr.sprite = _playerSprite;

                    // �v���C���[�̕`�揇����O�ɂ���
                    sr.sortingOrder = 2;

                    // �v���C���[�̈ʒu��ݒ�
                    _player.transform.position = GetDisplayPosition(x, y);

                    // �v���C���[��A�z�z��ɒǉ�
                    _gameObjectPosTable.Add(_player, new Vector2Int(x, y));
                }

                // �u���b�N�̏ꍇ
                else if (val == TileType.BLOCK)
                {
                    // �u���b�N�̐��𑝂₷
                    _blockCount++;

                    // �u���b�N�̃Q�[���I�u�W�F�N�g���쐬
                    GameObject block = new GameObject("block" + _blockCount);

                   // �u���b�N�ɃX�v���C�g��`�悷��@�\��ǉ�
                    sr = block.AddComponent<SpriteRenderer>();

                    // �u���b�N�̃X�v���C�g��ݒ�
                    sr.sprite = _blockSprite;

                    // �u���b�N�̕`�揇����O�ɂ���
                    sr.sortingOrder = 2;

                    // �u���b�N�̈ʒu��ݒ�
                    block.transform.position = GetDisplayPosition(x, y);

                    // �u���b�N��A�z�z��ɒǉ�
                    _gameObjectPosTable.Add(block, new Vector2Int(x, y));
                }
            }
        }
    }

    //�w�肳�ꂽ�s�ԍ��Ɨ�ԍ�����X�v���C�g�̕\���ʒu���v�Z���ĕԂ�
    private Vector2 GetDisplayPosition(int x, int y)
    {
        return new Vector2((x * _tileSize) - _middleOffset.x,
            (y * -_tileSize) + _middleOffset.y);
    }

    //-------------------------------------------------------------------------------------
    //���̉��q�ɔԂ̃��W�b�N
    #region �q�ɔԃ��W�b�N�i�`�F�b�N�֐��j
    //�w�肳�ꂽ�ʒu�ɑ��݂���Q�[���I�u�W�F�N�g��Ԃ�
    private GameObject GetGameObjectAtPosition(Vector2Int pos)
    {
        foreach (KeyValuePair<GameObject, Vector2Int> pair in _gameObjectPosTable)
        {
            // �w�肳�ꂽ�ʒu�����������ꍇ
            if (pair.Value == pos)
            {
                // ���̈ʒu�ɑ��݂���Q�[���I�u�W�F�N�g��Ԃ�
                return pair.Key;
            }
        }
        return null;
    }

    //�w�肳�ꂽ�ʒu�X�e�[�W���Ȃ�true��Ԃ�
    private bool IsValidPosition(Vector2Int pos)
    {
        if (0 <= pos.x && pos.x < _columns && 0 <= pos.y && pos.y < _rows)
        {
            return _tileList[pos.x, pos.y] != TileType.NONE;
        }
        return false;
    }

    //�w�肳�ꂽ�ʒu�̃^�C�����u���b�N�Ȃ�true��Ԃ�
    private bool IsBlock(Vector2Int pos)
    {
        TileType cell = _tileList[pos.x, pos.y];
        return cell == TileType.BLOCK || cell == TileType.BLOCK_ON_TARGET;
    }

    #endregion

    //-------------------------------------------------------------------------------------
    //���̉��ړ�����
    //�w�肳�ꂽ�����Ƀv���C���[���ړ��ł��邩����
    //�ړ��ł���ꍇ�͈ړ�����
    private void TryMovePlayer(DirectionType direction)
    {
        // �v���C���[�̌��ݒn���擾
        Vector2Int currentPlayerPos = _gameObjectPosTable[_player];

        // �v���C���[�̈ړ���̈ʒu���v�Z
        Vector2Int nextPlayerPos = GetNextPositionAlong(currentPlayerPos, direction);

        // �v���C���[�̈ړ��悪�X�e�[�W���ł͂Ȃ��ꍇ�͖���
        if (!IsValidPosition(nextPlayerPos))
        {
            return;
        } 

        // �v���C���[�̈ړ���Ƀu���b�N�����݂���ꍇ
        if (IsBlock(nextPlayerPos))
        {
            // �u���b�N�̈ړ���̈ʒu���v�Z
            Vector2Int nextBlockPos = GetNextPositionAlong(nextPlayerPos, direction);

            // �u���b�N�̈ړ��悪�X�e�[�W���̏ꍇ����
            // �u���b�N�̈ړ���Ƀu���b�N�����݂��Ȃ��ꍇ
            if (IsValidPosition(nextBlockPos) && !IsBlock(nextBlockPos))
            {
                // �ړ�����u���b�N���擾
                GameObject block = GetGameObjectAtPosition(nextPlayerPos);

                // �v���C���[�̈ړ���̃^�C���̏����X�V
                UpdateGameObjectPosition(nextPlayerPos);

                // �u���b�N���ړ�
                block.transform.position = GetDisplayPosition(nextBlockPos.x, nextBlockPos.y);

                // �u���b�N�̈ʒu���X�V
                _gameObjectPosTable[block] = nextBlockPos;

                // �u���b�N�̈ړ���̔ԍ����X�V
                if (_tileList[nextBlockPos.x, nextBlockPos.y] == TileType.GROUND)
                {
                    // �ړ��悪�n�ʂȂ�u���b�N�̔ԍ��ɍX�V
                    _tileList[nextBlockPos.x, nextBlockPos.y] = TileType.BLOCK;
                }
                else if (_tileList[nextBlockPos.x, nextBlockPos.y] == TileType.TARGET)
                {
                    // �ړ��悪�ړI�n�Ȃ�u���b�N�i�ړI�n�̏�j�̔ԍ��ɍX�V
                    _tileList[nextBlockPos.x, nextBlockPos.y] = TileType.BLOCK_ON_TARGET;
                }

                // �v���C���[�̌��ݒn�̃^�C���̏����X�V
                UpdateGameObjectPosition(currentPlayerPos);

                // �v���C���[���ړ�
                _player.transform.position = GetDisplayPosition(nextPlayerPos.x, nextPlayerPos.y);

                //�����J�E���g����
                _stepCount++;
                //�����^�ɕϊ�
                _stepCountText.text = _stepCount.ToString();

                // �v���C���[�̈ʒu���X�V
                _gameObjectPosTable[_player] = nextPlayerPos;

                // �v���C���[�̈ړ���̔ԍ����X�V
                if (_tileList[nextPlayerPos.x, nextPlayerPos.y] == TileType.GROUND)
                {
                    // �ړ��悪�n�ʂȂ�v���C���[�̔ԍ��ɍX�V
                    _tileList[nextPlayerPos.x, nextPlayerPos.y] = TileType.PLAYER;
                }
                else if (_tileList[nextPlayerPos.x, nextPlayerPos.y] == TileType.TARGET)
                {
                    // �ړ��悪�ړI�n�Ȃ�v���C���[�i�ړI�n�̏�j�̔ԍ��ɍX�V
                    _tileList[nextPlayerPos.x, nextPlayerPos.y] = TileType.PLAYER_ON_TARGET;
                }
            }
        }
        // �v���C���[�̈ړ���Ƀu���b�N�����݂��Ȃ��ꍇ
        else
        {
            // �v���C���[�̌��ݒn�̃^�C���̏����X�V
            UpdateGameObjectPosition(currentPlayerPos);

            // �v���C���[���ړ�
            _player.transform.position = GetDisplayPosition(nextPlayerPos.x, nextPlayerPos.y);

            // �v���C���[�̈ʒu���X�V
            _gameObjectPosTable[_player] = nextPlayerPos;

            // �v���C���[�̈ړ���̔ԍ����X�V
            if (_tileList[nextPlayerPos.x, nextPlayerPos.y] == TileType.GROUND)
            {
                // �ړ��悪�n�ʂȂ�v���C���[�̔ԍ��ɍX�V
                _tileList[nextPlayerPos.x, nextPlayerPos.y] = TileType.PLAYER;
            }
            else if (_tileList[nextPlayerPos.x, nextPlayerPos.y] == TileType.TARGET)
            {
                // �ړ��悪�ړI�n�Ȃ�v���C���[�i�ړI�n�̏�j�̔ԍ��ɍX�V
                _tileList[nextPlayerPos.x, nextPlayerPos.y] = TileType.PLAYER_ON_TARGET;
            }
            //�����J�E���g����
            _stepCount++;
            //�����^�ɕϊ�
            _stepCountText.text = _stepCount.ToString();
        }
        //�Q�[�����N���A�������ǂ����m�F
        CheckCompletion();
    }

    //�w�肳�ꂽ�����̈ʒu��Ԃ�
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
        // �w�肳�ꂽ�ʒu�̃^�C���̔ԍ����擾
        TileType cell = _tileList[pos.x, pos.y];

        // �v���C���[�������̓u���b�N�̏ꍇ
        if (cell == TileType.PLAYER || cell == TileType.BLOCK)
        {
            // �n�ʂɕύX
            _tileList[pos.x, pos.y] = TileType.GROUND;
        }
        // �ړI�n�ɏ���Ă���v���C���[�������̓u���b�N�̏ꍇ
        else if (cell == TileType.PLAYER_ON_TARGET || cell == TileType.BLOCK_ON_TARGET)
        {
            // �ړI�n�ɕύX
            _tileList[pos.x, pos.y] = TileType.TARGET;
        }
    }

    //�Q�[�����N���A�������ǂ����m�F
    private void CheckCompletion()
    {
        // �ړI�n�ɏ���Ă���u���b�N�̐����v�Z
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
        // ���ׂẴu���b�N���ړI�n�̏�ɏ���Ă���ꍇ
        if (blockOnTargetCount == _blockCount)
        {
            // �Q�[���N���A
            _isClear = true;
            //�Q�[���N���A�\��
            _rezultUi.gameObject.SetActive(true);
        }
    }

    //�N���A�����Ƃ��̃��g���C
    public void OnRetry()
    {
        _sceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(_sceneIndex);
    }

    //�N���A�����Ƃ��Ɏ��̃X�e�[�W�ֈړ�����
    public void OnNext()
    {
        _sceneIndex = SceneManager.GetActiveScene().buildIndex;
        //�i�[�����l����������Ď��̃V�[���Ɉړ�����
        SceneManager.LoadScene(_sceneIndex + 1);
    }

    //�Q�[���G���h
    public void OnEnd()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;//�Q�[���v���C�I��
#else
    Application.Quit();//�Q�[���v���C�I��
#endif
    }
}
