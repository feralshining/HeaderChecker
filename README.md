##  HeaderChecker with FileSignatures
>  파일의 바이너리 구조에서 Magic Number(시그니처)를 분석하여 파일의 확장자 적합성 여부를 파악하는 도구

### 💡 도구 사용법
1. 확장자 적합성을 파악할 파일들을 특정 폴더에 모아놓습니다.
2. 폴더 선택 버튼 클릭 후 폴더 지정
3. ListView에 파일 이름, 상태로 파일들의 확장자 적합성이 출력됨.
4. 분류가 완료되었을 경우, 지정했던 폴더 내에 OK, corrupted, mismatch, unknown 폴더로 각각 파일들이 분류되어 이동한 형태임.

- 기본적으로 FileSignature 라이브러리를 사용하여 분류하지만, 해당 라이브러리는 xlsx와 hwp파일에 대한 분석이 불가능하기 때문에, zip 파일 시그니처를 통해서 xlsx인지 아닌지 판단을 하는 추가적인 로직을 구현했음.

### 📌 참고사항

- `File Carving 기법으로 살려낸 조각 파일들이나, FTK Imager 같은 포렌식 도구를 통해 확인할 수 있는 orphan files들의 확장자를 정확하게 분류해내기 위해서 급하게 만든 도구임. 깔끔하고 정확하게 모든 파일 시그니처를 분석해서 분류하고 싶으면, 코드를 정리해야함. pdf, hwp, xlsx같은 document 파일을 대상으로 사용하기에는 무리가 없음.`

### 📕 사용 라이브러리
- **File Signature** : https://github.com/neilharvey/FileSignatures/ 
- `thanks for Neil Harvey`

### 🖥️ 실행환경
- **.NET Framework 4.7.2**
