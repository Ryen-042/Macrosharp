.PHONY: classlib rmproj build b compile c clean clean-build rebuild rb cb cc run build-run br restore

.DEFAULT_GOAL := run


# For some reason, the globstar (eg, **/*.cs) is broken in windows. This is a workaround.
# Source: https://stackoverflow.com/questions/2483182/recursive-wildcards-in-gnu-make
# Other: https://dev.to/blikoor/customize-git-bash-shell-498l
rwildcard=$(foreach d,$(wildcard $(1:=/*)),$(call rwildcard,$d,$2) $(filter $(subst *,%,$2),$d))

%:
	@:

classlib: src/Macrosharp.sln
	@if [ "$(words $(filter-out $@,$(MAKECMDGOALS)))" != "1" ]; then \
		echo "Error: Exactly one project name is required. Usage: make classlib <SubfolderName.ProjectName> or make classlib <ProjectName>"; \
		exit 1; \
	fi

	$(eval FULL_NAME := $(filter-out $@,$(MAKECMDGOALS)))
	$(eval PROJECT_NAME := $(shell echo "$(FULL_NAME)" | awk -F'.' '{print $$NF}'))

	@if [ -z "$(PROJECT_NAME)" ]; then \
		echo "Error: Invalid project name"; \
		exit 2; \
	fi

	$(eval PROJECT_FULL_NAME := Macrosharp.$(FULL_NAME))
	$(eval TARGET_DIR := $(shell \
		full_name="$(FULL_NAME)"; \
		if echo "$$full_name" | grep -q '\.'; then \
			project_name="$${full_name##*.}"; \
			dir_parts="$${full_name%.*}"; \
			components=$$(echo "$$dir_parts" | tr '.' ' '); \
			current="Macrosharp"; \
			path="src"; \
			for part in $$components; do \
				current="$$current.$$part"; \
				path="$$path/$$current"; \
			done; \
			current="$$current.$$project_name"; \
			path="$$path/$$current"; \
			echo "$$path"; \
		else \
			echo "src/Macrosharp.$$full_name"; \
		fi; \
	))

	@if [ -d "$(TARGET_DIR)" ]; then \
		echo "Project directory '$(TARGET_DIR)' already exists."; \
		exit 3; \
	fi

	@mkdir -p $(TARGET_DIR)
	@dotnet new classlib -n $(PROJECT_FULL_NAME) -o $(TARGET_DIR)
	@dotnet sln src/Macrosharp.sln add $(TARGET_DIR)/$(PROJECT_FULL_NAME).csproj
	@echo "Project '$(PROJECT_FULL_NAME)' created successfully."


# Command to remove a project from the solution
rmproj: src/Macrosharp.sln
	@# Check if exactly one argument is provided
	@if [ "$(words $(filter-out $@,$(MAKECMDGOALS)))" != "1" ]; then \
		echo "Error: Exactly one project name is required. Usage: make rmproj <SubfolderName.ProjectName> or make rmproj <ProjectName>"; \
		exit 1; \
	fi

	@# Extract the project name from the argument. Its the last part after the last dot for subprojects, or the whole argument for top-level projects.
	$(eval FULL_NAME := $(filter-out $@,$(MAKECMDGOALS)))
	$(eval PROJECT_NAME := $(shell echo "$(FULL_NAME)" | awk -F'.' '{print $$NF}'))

	@# Check if the project name is not empty.
	@if [ -z "$(PROJECT_NAME)" ]; then \
		echo "Error: Invalid project name: $(PROJECT_NAME)"; \
		exit 2; \
	fi

	@# Check if the project is a subproject or a top-level project.
	$(eval HAS_SUBDIRS := $(shell echo "$(FULL_NAME)" | grep -q '\.' && echo true || echo false))

	@# Construct the full project name and the target directory path, making sure to handle subprojects correctly.
	$(eval PROJECT_FULL_NAME := $(if $(filter $(HAS_SUBDIRS),true),Macrosharp.$(FULL_NAME),Macrosharp.$(PROJECT_NAME)))
	$(eval TARGET_DIR := $(shell \
		full_name="$(FULL_NAME)"; \
		if echo "$$full_name" | grep -q '\.'; then \
			project_name="$${full_name##*.}"; \
			dir_parts="$${full_name%.*}"; \
			components=$$(echo "$$dir_parts" | tr '.' ' '); \
			current="Macrosharp"; \
			path="src"; \
			for part in $$components; do \
				current="$$current.$$part"; \
				path="$$path/$$current"; \
			done; \
			current="$$current.$$project_name"; \
			path="$$path/$$current"; \
			echo "$$path"; \
		else \
			echo "src/Macrosharp.$$full_name"; \
		fi; \
	))

	@# Construct the full path to the .csproj.
	$(eval PROJECT_FILE := $(TARGET_DIR)/$(PROJECT_FULL_NAME).csproj)

	@# Check if the project directory and the .csproj file exist.
	@if [ ! -d "$(TARGET_DIR)" ] || [ ! -f "$(PROJECT_FILE)" ]; then \
		echo "Error: Project directory or .csproj file does not exist."; \
		echo "Project directory: $(TARGET_DIR)"; \
		echo "Project file Path: $(PROJECT_FILE)"; \
		exit 3; \
	fi

	@dotnet sln src/Macrosharp.sln remove $(PROJECT_FILE)
	@sleep 1
	@rm -rf $(TARGET_DIR)
	@echo "Project '$(PROJECT_FULL_NAME)' and its directory have been removed successfully."

	@# Recursively check and delete empty directories starting from the deepest level up to the 'src' directory
	@PARENT_DIR="$(shell dirname "$(TARGET_DIR)")"; \
	while [ "$$PARENT_DIR" != "src" ]; do \
		echo "Checking directory: $$PARENT_DIR"; \
		if [ -d "$$PARENT_DIR" ] && [ -z "$$(ls -A "$$PARENT_DIR" 2>/dev/null)" ]; then \
			echo "Deleting empty directory: $$PARENT_DIR"; \
			rmdir "$$PARENT_DIR"; \
		else \
			echo "Directory not empty or does not exist: $$PARENT_DIR"; \
			break; \
		fi; \
		PARENT_DIR="$$(dirname "$$PARENT_DIR")"; \
	done

build: src/Macrosharp.sln
	dotnet build src/Macrosharp.sln
compile: build  # Alias
b: build  # Alias (b = build)
c: build  # Alias (c = compile)

clean: src/Macrosharp.sln
	dotnet clean src/Macrosharp.sln

clean-build: clean build
cb: clean-build  # Alias (cb = clean-build)
cc: clean-build  # Alias (cc = clean-compile)
rb: rebuild      # Alias (rb = rebuild)
rebuild: clean-build # Alias

run: src/Macrosharp.sln src/Macrosharp.Hosts/Macrosharp.Hosts.Console/Macrosharp.Hosts.Console.csproj
	dotnet run --project src/Macrosharp.Hosts/Macrosharp.Hosts.Console/Macrosharp.Hosts.Console.csproj

build-run: build run
br: build-run  # Alias (br = build-run)

restore: src/Macrosharp.sln
	dotnet restore src/Macrosharp.sln
