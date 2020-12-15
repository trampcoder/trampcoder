using System.Collections;
using System.IO;
using UnityEngine;

namespace Tools
{
    public class ScreenCapture : MonoBehaviour
    {
        public KeyCode keyCode;                     // клавиша для записи
        public string directory = "ScreenCapture";  // имя директории внутри директории проекта
        public string filename = "level_";          // название файла картинки
        public Transform leftBottomStartMarker;     // начало сканирования сцены
        public Transform rightTopEndMarker;         // окончание сканирования

        private Camera _camera;                     // компонент камеры на данном объекте
        private RenderTexture _renderTexture;       // RenderTexture на камере
        private int _height;                        // высота экрана (значение из RenderTexture)
        private int _width;                         // ширина экрана (значение из RenderTexture)
        private Vector3 _startPosition;             // стартовая позиция объекта
        private int _heightCapture;                 // высота результирующей текстуры
        private int _widthCapture;                  // длина результирующей текстуры
        private bool _capturing = false;            // процесс запущен
        private int _stepsUp = 0;                   // количество проходов вверх
        private int _stepsRight = 0;                // количество проходов вправо

        private void Start()
        {
            _camera = GetComponent<Camera>();

            // RenderTexture задает настройки экрана
            _renderTexture = _camera.targetTexture;
            _height = _renderTexture.height;
            _width = _renderTexture.width;
           
            if (_width != Screen.width || _height != Screen.height) {
                Screen.SetResolution(_width, _height, Screen.fullScreenMode);
            }

            // начальная позиция - координаты данного объекта 
            // или позиция объекта левой нижней метки
            _startPosition = leftBottomStartMarker == null ? 
                             transform.position :
                             leftBottomStartMarker.position;

            _startPosition.x = Mathf.RoundToInt(_startPosition.x);
            _startPosition.y = Mathf.RoundToInt(_startPosition.y);

            // количество шагов прохода (количество экранов) и 
            // размеры результирующей текстуры, 
            // если отсутствует правый верхний маркер
            _stepsRight = 1;
            _stepsUp = 1;
            _widthCapture = _width;
            _heightCapture = _height;

            // если правый верхний маркер есть, рассчитать шаги/размеры
            if (rightTopEndMarker != null)
            {
                Vector2 right = rightTopEndMarker.position;
                right.x = Mathf.CeilToInt(right.x);
                right.y = Mathf.CeilToInt(right.y);

                float widthCapture = right.x - _startPosition.x + (_width * 0.5f);
                float heightCapture = right.y - _startPosition.y + (_height * 0.5f);

                _stepsRight = Mathf.CeilToInt((widthCapture / _width));
                _stepsUp = Mathf.CeilToInt((heightCapture / _height));

                _widthCapture = _stepsRight * _width;
                _heightCapture = _stepsUp * _height;
            }
        }

        private void Update()
        {
            if (_capturing) { return; }

            if (Input.GetKeyDown(keyCode))
            {
                _capturing = true;
                StartCoroutine(Capturing());
            }

            // скрипт сработает один раз
            // кому нужно будет многоразовый вызов (хотя не знаю зачем)
            // _capturing сбрасывать в конце Capturing
            // и перенести Start в Capturing (начиная с _startPosition)
        }

        /// <summary>
        /// Вытащить текстуру из того, что видит камера
        /// </summary>
        Texture2D GetCameraTexture()
        {      
            RenderTexture.active = _renderTexture;

            _camera.Render();
           
            Texture2D image = new Texture2D(_width, _height) {
                filterMode = _renderTexture.filterMode,
                wrapMode = _renderTexture.wrapMode
            }; 
            image.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            image.Apply();

            RenderTexture.active = null;

            return image;
        }

        /// <summary>
        /// Основной процесс захвата
        /// </summary> 
        IEnumerator Capturing()
        {            
            Texture2D resultTexture = new Texture2D(_widthCapture, _heightCapture);
            resultTexture.filterMode = _renderTexture.filterMode;
            resultTexture.wrapMode = _renderTexture.wrapMode;

            // 1 - передвинуть камеру
            // 2 - вытащить текстуру
            // 3 - записать её в результирующую текстуру           

            for (int h = 0; h < _stepsUp; h++)                
            {
                for (int w = 0; w < _stepsRight; w++)
                {
                    // ставлю задержку перед перемещением камеры, 
                    // чтобы видеть как объект прыгает по сцене в редакторе
                    yield return new WaitForSeconds(0.2f);
                    
                    Vector3Int step = new Vector3Int() {
                        x = _width * w,
                        y = _height * h
                    };

                    transform.position = _startPosition + step;

                    Texture2D texture = GetCameraTexture();
                    resultTexture.SetPixels(step.x, step.y, _width, _height, texture.GetPixels());                     
                }
            }

            // завершить работу над результирующей текстурой и сохранить ее в файл
            resultTexture.Apply();
            Save(resultTexture);

            // если нет задержки WaitForSeconds во время перемещения камеры, 
            // то для Coroutine нужен вызов 
            // yield return new WaitForEndOfFrame();
        }        

        /// <summary>
        /// Сохранить текстуру в файл
        /// </summary>
        public void Save(Texture2D texture)
        {
            string path = Application.dataPath + "/../" + directory + "/";
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }

            string filepath = path + filename + ".png";
            byte[] _bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(filepath, _bytes);
        }
    }
}
