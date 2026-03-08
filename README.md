# ThermalCamera

USB 열화상 카메라에서 영상을 가져와 좌측 상단의 고정 길이 실수 온도값(예: 21.2 °C)을 OCR로 인식하는 Windows Forms 앱입니다.

## 프로젝트 구조

- **SThermalCamera** – USB 카메라 열거/연결/해제, 프레임 캡처, 좌측 상단 영역 OCR로 온도 인식
- **SLog** – RichTextBox 로그 출력 및 파일 저장
- **MainForm** – ListBox(카메라 목록), Connect/Disconnect/캡처·온도인식 버튼, 온도 TextBox, 로그 RichTextBox

## OCR (tessdata) 설정

온도 텍스트 인식을 위해 **Tesseract** 영어 데이터가 필요합니다.

1. [tesseract-ocr/tessdata](https://github.com/tesseract-ocr/tessdata) 에서 `eng.traineddata` 다운로드
2. 실행 파일과 같은 위치에 `tessdata` 폴더를 만들고 그 안에 `eng.traineddata` 넣기  
   - 예: `ThermalCamera\bin\Debug\net8.0-windows\tessdata\eng.traineddata`
3. 또는 `SThermalCamera.TessDataPath`에 tessdata 폴더 경로를 지정

tessdata가 없으면 카메라는 동작하지만 온도 인식 결과는 나오지 않습니다.

## 빌드 및 실행

```bash
cd c:\ws\ThermalCamera
dotnet build
dotnet run --project ThermalCamera
```

## Form 컨트롤 요약

| 컨트롤 | 용도 |
|--------|------|
| ListBox | USB 열화상 카메라 목록 |
| Button 1 | Connect |
| Button 2 | Disconnect |
| Button 3 | Image 가져오기 및 온도 인식 |
| TextBox | 인식된 온도값 표시 |
| RichTextBox | 로그 출력 |
| PictureBox | 캡처된 열화상 이미지 미리보기 |
