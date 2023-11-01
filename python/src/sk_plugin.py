from pathlib import Path

from semantic_kernel.sk_pydantic import SKBaseModel

from python.src.sk_function import SKFunction


class SKPlugin(SKBaseModel):
    name: str
    folder: str
    native: bool = False
    yaml: bool = False
    yaml_files: list = []
    native_files: list = []
    functions: dict[str, SKFunction] = {}

    def __init__(
        self, name: str, folder: str, native: bool = False, yaml: bool = False
    ):
        super().__init__(name=name, folder=folder, native=native, yaml=yaml)
        if not self.native and not self.yaml:
            raise ValueError("Either native or yaml or both must be True")
        # go through the folder and find all yaml files in subfolders
        path = Path(self.folder)
        for file in path.iterdir():
            if file.suffix == ".yaml" and self.yaml:
                self.yaml_files.append(file)
            if file.suffix == ".py" and self.native:
                self.native_files.append(file)
        # create skfunctions from yaml files
        for file in self.yaml_files:
            try:
                func = SKFunction.from_yaml(file)
                self.functions[func.name] = func
            except Exception as e:
                print(f"Error while parsing yaml file {file}: {e}")
                continue
        # create skfunctions from python files
