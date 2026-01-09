document.addEventListener('DOMContentLoaded', function() {
    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }
    
    const uploadTriggers = document.querySelectorAll('.js-upload-trigger');
    
    uploadTriggers.forEach(trigger => {
        const pageId = trigger.getAttribute('data-page');
        const modal = document.getElementById(`upload-modal-${pageId}`);
        
        if (!modal) return;
        
        const closeButton = modal.querySelector('.js-modal-close');
        const dropArea = modal.querySelector('.js-drop-area');
        const fileInput = modal.querySelector('.js-file-input');
        const fileList = modal.querySelector('.js-file-list');
        const submitButton = modal.querySelector('.js-upload-submit');
        
        trigger.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            modal.style.display = 'block';
        });
        
        closeButton.addEventListener('click', () => {
            modal.style.display = 'none';
        });
        
        window.addEventListener('click', (event) => {
            if (event.target === modal) {
                modal.style.display = 'none';
            }
        });
        
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            dropArea.addEventListener(eventName, (e) => {
                e.preventDefault();
                e.stopPropagation();
            });
        });
        
        ['dragenter', 'dragover'].forEach(eventName => {
            dropArea.addEventListener(eventName, () => {
                dropArea.classList.add('active');
            });
        });
        
        ['dragleave', 'drop'].forEach(eventName => {
            dropArea.addEventListener(eventName, () => {
                dropArea.classList.remove('active');
            });
        });
        
        dropArea.addEventListener('drop', (e) => {
            const files = e.dataTransfer.files;
            displayFiles(files, fileList);
        });
        
        dropArea.addEventListener('click', () => {
            fileInput.click();
        });
        
        fileInput.addEventListener('change', () => {
            displayFiles(fileInput.files, fileList);
        });
        
        function displayFiles(files, container) {
            container.innerHTML = '';
            Array.from(files).forEach(file => {
                const fileItem = document.createElement('div');
                fileItem.className = 'file-item';
                fileItem.innerHTML = `
                    <div>${file.name}</div>
                    <div>${formatFileSize(file.size)}</div>
                `;
                container.appendChild(fileItem);
            });
        }
        
        let isUploading = false;

        submitButton.addEventListener('click', () => {
            if (isUploading) {
            console.log('Upload already in progress, ignoring click');
            return;
        }    

            const files = fileInput.files;
            
            if (files.length === 0) {
                alert('Please select files to upload');
                return;
            }
            
            isUploading = true;
            submitButton.disabled = true;
            submitButton.classList.add('disabled');
            
            const formData = new FormData();
            Array.from(files).forEach(file => {
                formData.append('file', file);
            });
            
            const existingStatus = fileList.querySelector('.upload-status');
        if (existingStatus) {
            fileList.removeChild(existingStatus);
        }
            const statusContainer = document.createElement('div');
            statusContainer.className = 'upload-status';
            statusContainer.textContent = 'Uploading...';
            fileList.appendChild(statusContainer);
            
                fetch('/api/files/upload', {
                method: 'POST',
                body: formData
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Upload failed: ' + response.statusText);
                }
                return response.json();
            })
            .then(data => {
                statusContainer.textContent = 'Upload successful!';
                statusContainer.style.color = 'green';
                
                    setTimeout(() => {
                    fileInput.value = '';
                    fileList.innerHTML = '';
                    modal.style.display = 'none';
                    
                    isUploading = false;
                    submitButton.disabled = false;
                    submitButton.classList.remove('disabled');
                }, 2000);
            })
            .catch(error => {
                statusContainer.textContent = error.message;
                statusContainer.style.color = 'red';
                
                isUploading = false;
                submitButton.disabled = false;
                submitButton.classList.remove('disabled');
            });
        });
    });
});

function loadSearchResults(query) {
    const searchResultsContainer = document.getElementById('search-results');
    const resultsNumber = document.getElementById('results-number');
    
    if (!searchResultsContainer) return;
    
    if (!query) {
        window.location.href = '/';
        return;
    }
    
    fetch(`/api/search?q=${encodeURIComponent(query)}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Search failed');
            }
            return response.json();
        })
        .then(data => {
            if (data.length === 0) {
                searchResultsContainer.innerHTML = '<div class="no-results">No results found</div>';
                resultsNumber.textContent = '0';
                return;
            }
            
            searchResultsContainer.innerHTML = '';
            data.forEach(result => {
                const resultItem = document.createElement('div');
                resultItem.className = 'result-item';
                resultItem.innerHTML = `
                    <div class="result-title">
                        <a href="/api/files/${result.documentId}">${result.fileName}</a>
                    </div>
                    <div class="result-path">${result.filePath}</div>
                `;
                
                            const snippetDiv = document.createElement('div');
                snippetDiv.className = 'result-snippet';
                snippetDiv.innerHTML = result.snippet;                
                            resultItem.appendChild(snippetDiv);
                
                            searchResultsContainer.appendChild(resultItem);
            });
            
            if (resultsNumber) {
                resultsNumber.textContent = data.length;
            }
        })
        .catch(error => {
            searchResultsContainer.innerHTML = `<div class="search-error">Error: ${error.message}</div>`;
        });
}
